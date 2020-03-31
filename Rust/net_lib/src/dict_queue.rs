use crate::smart_monitor::{smlogger, SMLogger};
use crate::tcp_listener::TcpConnManager;
use crossbeam::channel::{unbounded, Receiver, Sender};
use serde::Deserialize;
use std::collections::BTreeMap;

struct DictOutputQueue<T, U> {
    producer: Sender<(T, U)>,
    consumer: Receiver<(T, U)>,
}

impl<T, U> DictOutputQueue<T, U>
where
    T: Ord + Clone,
    U: Clone + PartialEq,
{
    fn new() -> Self {
        let (producer, consumer) = unbounded();
        Self { producer, consumer }
    }

    fn send(&self, item: T, value: U) {
        self.producer.send((item, value)).unwrap();
    }

    fn new_consumer(&self) -> DictInputQueue<T, U> {
        DictInputQueue::<T, U>::new(self.consumer.clone())
    }
}

struct DictInputQueue<T, U> {
    consumer: Receiver<(T, U)>,
    values: BTreeMap<T, U>,
    last_value: Option<(T, U)>,
}

impl<T, U> DictInputQueue<T, U>
where
    T: Ord + Clone,
    U: Clone + PartialEq,
{
    fn new(consumer: Receiver<(T, U)>) -> Self {
        Self {
            consumer,
            values: BTreeMap::new(),
            last_value: None,
        }
    }

    fn next(&mut self) -> &(T, U) {
        while let Ok(v) = self.consumer.recv() {
            if let Some(old_value) = self.values.insert(v.0.clone(), v.1.clone()) {
                if old_value != v.1 {
                    self.last_value = Some(v);
                    break;
                }
            } else {
                self.last_value = Some(v);
                break;
            }
        }
        self.last_value.as_ref().unwrap()
    }

    fn get(&mut self, key: &T) -> Option<&U> {
        // Consume any available messages first
        while let Ok(v) = self.consumer.recv() {
            self.values.insert(v.0.clone(), v.1.clone());
            if self.consumer.is_empty() {
                break;
            }
        }
        self.values.get(key)
    }
}

/// A TCP scalar output queue pushes a single item of T to it's one or more receivers
/// The dicovery service will notify this service of what it's potential queue routes are
/// If you have a tcp queue, it cannot be used as an internal queue at the same time currently,
/// as it's determined at compile time
struct TcpDictOutputQueue<T, U> {
    consumer: Receiver<Vec<u8>>,
    consumer_typed: Receiver<(T, U)>,
    producer_typed: Sender<(T, U)>,
    _phantom: std::marker::PhantomData<T>,
}

impl<T, U> TcpDictOutputQueue<T, U>
where
    T: for<'de> Deserialize<'de> + Ord + Clone,
    U: for<'de> Deserialize<'de> + Clone + PartialEq,
{
    fn new(mgr: &mut TcpConnManager, channel: &'static str) -> Self {
        let (producer, consumer) = unbounded();
        mgr.add_producer(channel, producer);
        let (producer_typed, consumer_typed) = unbounded();
        Self {
            consumer,
            consumer_typed,
            producer_typed,
            _phantom: std::marker::PhantomData,
        }
    }

    fn send(&self) {
        if let Ok(v) = self.consumer.recv() {
            let v = bincode::deserialize::<(T, U)>(&v).expect("Could not deserialise from bytes");
            self.producer_typed.send(v).unwrap();
        }
    }
    fn new_consumer(&self) -> DictInputQueue<T, U> {
        DictInputQueue::<T, U>::new(self.consumer_typed.clone())
    }
}
/*
// Output Queue
pub struct DictOutputQueue<T, U>
where
    T: Clone + for<'de> Deserialize<'de> + Serialize + Send + Ord + 'static,
    U: for<'de> Deserialize<'de> + Serialize + Send + 'static,
{
    dict_consumers: Vec<DictInputQueue<T, U>>,
    service: &'static str,
    _data: std::marker::PhantomData<(T, U)>,
}

pub struct TcpDictOutputQueue<T, U>
where
    T: Serialize + Send + 'static,
    U: Serialize + Send + 'static,
{
    channel_id: &'static str,
    sender: mpsc::Sender<(&'static str, Vec<u8>)>,
    sm_log: SMLogger<(T, U)>,
}

impl<T, U> TcpDictOutputQueue<T, U>
where
    T: Serialize + Send + 'static,
    U: Serialize + Send + 'static,
{
    pub fn new(
        channel_id: &'static str,
        service: &'static str,
        sender: &mpsc::Sender<(&'static str, Vec<u8>)>, // Move this to a singleton?
    ) -> Self {
        Self {
            channel_id,
            sender: sender.clone(),
            sm_log: smlogger().create_sender::<(T, U)>(channel_id, service),
        }
    }

    pub fn send(&mut self, item: T, value: U) {
        let bytes = bincode::serialize(&item).unwrap();
        self.sm_log.entry(&(item, value));
        self.sender.try_send((self.channel_id, bytes)).unwrap();
        self.sm_log.exit();
    }
}

// Implmentation of Output queue
// Split this into type specific queues
impl<T, U> DictOutputQueue<T, U>
where
    T: Clone + Ord + Serialize + Send + for<'de> Deserialize<'de> + 'static,
    U: Clone + Serialize + Send + for<'de> Deserialize<'de> + 'static,
{
    pub fn new(service: &'static str) -> Self {
        Self {
            dict_consumers: Vec::new(),
            service: service,
            _data: std::marker::PhantomData,
        }
    }

    pub fn dict_input_queue(&'static mut self, channel_id: &'static str) -> &DictInputQueue<T, U> {
        self.dict_consumers
            .push(DictInputQueue::<T, U>::new(channel_id, &self.service));
        self.dict_consumers.last().unwrap()
    }

    pub fn send(&mut self, item: T, value: U) {
        for output in self.dict_consumers.iter_mut() {
            output.push(item.clone(), value.clone());
        }
    }
}

pub struct DictInputQueue<T, U>
where
    T: Serialize + for<'de> Deserialize<'de> + PartialOrd + Ord,
    U: Serialize + for<'de> Deserialize<'de>,
{
    last_value: Arc<Mutex<Option<(T, U)>>>,
    values: BTreeMap<T, u64>,
    sm_log: SMLogger<(T, U)>,
}

impl<T, U> DictInputQueue<T, U>
where
    T: Clone + Serialize + for<'de> Deserialize<'de> + PartialOrd + Ord,
    U: Clone + Serialize + for<'de> Deserialize<'de>,
{
    fn new(channel_id: &'static str, service: &'static str) -> Self {
        Self {
            last_value: Arc::new(Mutex::new(None)),
            values: BTreeMap::new(),
            sm_log: smlogger().create_sender::<(T, U)>(channel_id, service),
        }
    }

    pub fn push(&mut self, item: T, value: U) {
        if !self.values.contains_key(&item) {
            self.values.insert(item.clone(), 0);
            let mut data = self.last_value.try_lock().unwrap();
            self.sm_log.entry(&(item.clone(), value.clone()));
            *data = Some((item, value));
        }
    }
}

impl<T, U> Future for DictInputQueue<T, U>
where
    T: Serialize + std::marker::Unpin + Copy + for<'de> Deserialize<'de> + Ord,
    U: Serialize + std::marker::Unpin + for<'de> Deserialize<'de> + Copy,
{
    type Output = (T, U);
    fn poll(mut self: Pin<&mut Self>, ctx: &mut Context) -> Poll<Self::Output> {
        if let Some(value) = {
            self.last_value
                .try_lock()
                .map_or_else(|| None, |v| v.or(None))
        } {
            self.borrow_mut().sm_log.exit();
            Poll::Ready(value)
        } else {
            ctx.waker().wake_by_ref();
            Poll::Pending
        }
        // if let Some(value) = self
        //     .last_value
        //     .try_lock()
        //     .map_or_else(|| None, |v| v.or(None))
        // {
        //     self.borrow_mut().sm_log.exit();
        //     Poll::Ready(value)
        // } else {
        //     ctx.waker().wake_by_ref();
        //     Poll::Pending
        // }
    }
}

impl<T, U> PushByteData for DictInputQueue<T, U>
where
    T: Clone + Serialize + for<'de> Deserialize<'de> + Ord + Send,
    U: Clone + Serialize + for<'de> Deserialize<'de> + Send,
{
    fn push_data(&mut self, v: &[u8]) {
        let v = bincode::deserialize::<(T, U)>(v).expect("Could not deserialise from bytes");
        self.push(v.0, v.1);
    }
}

struct DictOutputQueue<T, U> {
    producer: Sender<(T, U)>,
    consumer: Receiver<(T, U)>,
}

impl<T, U> DictOutputQueue<T, U>
where
    T: Ord + Clone,
    U: Clone + PartialEq,
{
    fn new() -> Self {
        let (producer, consumer) = unbounded();
        Self { producer, consumer }
    }

    fn send(&self, item: T, value: U) {
        self.producer.send((item, value)).unwrap();
    }

    fn new_consumer(&self) -> DictInputQueue<T, U> {
        DictInputQueue::<T, U>::new(self.consumer.clone())
    }
}
*/

#[cfg(test)]
mod test {
    use super::*;
    use crossbeam::thread::scope;

    struct DictProducer {
        out1: DictOutputQueue<i32, i32>,
    }

    impl DictProducer {
        fn new() -> Self {
            Self {
                out1: DictOutputQueue::new(),
            }
        }

        fn run(&self) {
            for i in &[0, 1, 1, 1, 1, 2, 2, 2, 3, 4] {
                self.out1.send(*i, *i * 2);
            }
        }
    }

    struct DictConsumer {
        in1: DictInputQueue<i32, i32>,
    }

    impl DictConsumer {
        fn new(in1: DictInputQueue<i32, i32>) -> Self {
            Self { in1 }
        }
        fn run(&mut self) {
            loop {
                DictConsumer::run_impl(&mut self.in1);
            }
        }

        fn run_impl(in1: &mut DictInputQueue<i32, i32>) {
            println!("In1 {:?}", in1.next());
        }
    }
    #[test]
    fn cross_beam_test_dict() {
        let producer = DictProducer::new();
        let mut consumer = DictConsumer::new(producer.out1.new_consumer());
        scope(|scope| {
            scope.spawn(|_| {
                producer.run();
            });
            consumer.run();
        })
        .unwrap();
    }
}
