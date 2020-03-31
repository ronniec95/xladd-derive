use crate::tcp_listener::TcpConnManager;
use crossbeam::channel::{unbounded, Receiver, Sender};
use serde::Deserialize;
use std::collections::BTreeSet;

struct SetOutputQueue<T> {
    producer: Sender<T>,
    consumer: Receiver<T>,
}

impl<T> SetOutputQueue<T>
where
    T: Ord + Clone,
{
    fn new() -> Self {
        let (producer, consumer) = unbounded();
        Self { producer, consumer }
    }

    fn send(&self, item: T) {
        self.producer.send(item).unwrap();
    }

    fn new_consumer(&self) -> SetInputQueue<T> {
        SetInputQueue::<T>::new(self.consumer.clone())
    }
}

struct SetInputQueue<T> {
    consumer: Receiver<T>,
    values: BTreeSet<T>,
    last_value: Option<T>,
}

impl<'a, T> SetInputQueue<T>
where
    T: Ord + Clone,
{
    fn new(consumer: Receiver<T>) -> Self {
        Self {
            consumer,
            values: BTreeSet::new(),
            last_value: None,
        }
    }

    fn next(&mut self) -> &T {
        while let Ok(v) = self.consumer.recv() {
            if self.values.insert(v.clone()) {
                self.last_value = Some(v);
                break;
            }
        }
        self.last_value.as_ref().unwrap()
    }
}

/// A TCP scalar output queue pushes a single item of T to it's one or more receivers
/// The dicovery service will notify this service of what it's potential queue routes are
/// If you have a tcp queue, it cannot be used as an internal queue at the same time currently,
/// as it's determined at compile time
struct TcpSetOutputQueue<T> {
    consumer: Receiver<Vec<u8>>,
    consumer_typed: Receiver<T>,
    producer_typed: Sender<T>,
    _phantom: std::marker::PhantomData<T>,
}

impl<T> TcpSetOutputQueue<T>
where
    T: for<'de> Deserialize<'de> + Ord + Clone,
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
            let v = bincode::deserialize::<T>(&v).expect("Could not deserialise from bytes");
            self.producer_typed.send(v).unwrap();
        }
    }
    fn new_consumer(&self) -> SetInputQueue<T> {
        SetInputQueue::<T>::new(self.consumer_typed.clone())
    }
}

/*
// Output Queue
pub struct SetOutputQueue<T: Clone + Serialize + Send + Ord + 'static> {
    set_consumers: Vec<SetInputQueue<T>>,
    service: &'static str,
}

pub struct SetInputQueue<T>
where
    T: Serialize + PartialOrd + Ord,
{
    last_value: Arc<Mutex<Option<T>>>,
    values: BTreeSet<T>,
    sm_log: SMLogger<T>,
}

pub struct TcpSetOutputQueue<T>
where
    T: Serialize + Send + 'static,
{
    channel_id: &'static str,
    sender: mpsc::Sender<(&'static str, Vec<u8>)>,
    sm_log: SMLogger<T>,
}

impl<T> TcpSetOutputQueue<T>
where
    T: Serialize + Send + 'static,
{
    pub fn new(
        channel_id: &'static str,
        service: &'static str,
        sender: &mpsc::Sender<(&'static str, Vec<u8>)>, // Move this to a singleton?
    ) -> Self {
        Self {
            channel_id,
            sender: sender.clone(),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    pub fn send(&mut self, item: T) {
        self.sm_log.entry(&item);
        let bytes = bincode::serialize(&item).unwrap();
        self.sender.try_send((self.channel_id, bytes)).unwrap();
        self.sm_log.exit();
    }
}

// Implmentation of Output queue
// Split this into type specific queues
impl<T> SetOutputQueue<T>
where
    T: Clone + Ord + Serialize + Send + for<'de> Deserialize<'de> + std::fmt::Debug + 'static,
{
    pub fn new(service: &'static str) -> Self {
        Self {
            set_consumers: Vec::new(),
            service: service,
        }
    }

    pub fn set_input_queue(&'static mut self, channel_id: &'static str) -> &SetInputQueue<T> {
        self.set_consumers
            .push(SetInputQueue::new(channel_id, &self.service));
        self.set_consumers.last().unwrap()
    }

    pub fn send(&mut self, item: T) {
        for output in self.set_consumers.iter_mut() {
            output.push(item.clone());
        }
    }
}

impl<T> SetInputQueue<T>
where
    T: Clone + Serialize + for<'de> Deserialize<'de> + PartialOrd + Ord,
{
    fn new(channel_id: &'static str, service: &'static str) -> Self {
        Self {
            last_value: Arc::new(Mutex::new(None)),
            values: BTreeSet::new(),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    pub fn push(&mut self, item: T) {
        if !self.values.contains(&item) {
            let mut data = self.last_value.try_lock().unwrap();
            self.sm_log.entry(&item);
            self.values.insert(item.clone());
            *data = Some(item);
        }
    }
}

impl<T> Future for SetInputQueue<T>
where
    T: Serialize + std::marker::Unpin + for<'de> Deserialize<'de> + Ord + Copy,
{
    type Output = T;
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
    }
}

impl<T> PushByteData for SetInputQueue<T>
where
    T: Clone + Serialize + for<'de> Deserialize<'de> + Ord + Send,
{
    fn push_data(&mut self, v: &[u8]) {
        let v = bincode::deserialize::<T>(v).expect("Could not deserialise from bytes");
        self.push(v);
    }
}
*/

#[cfg(test)]
mod test {
    use super::*;
    use crossbeam::thread::scope;

    struct SetProducer {
        out1: SetOutputQueue<i32>,
    }

    impl SetProducer {
        fn new() -> Self {
            Self {
                out1: SetOutputQueue::new(),
            }
        }

        fn run(&self) {
            for i in &[0, 1, 1, 1, 1, 2, 2, 2, 3, 4] {
                self.out1.send(*i);
            }
        }
    }

    struct SetConsumer {
        in1: SetInputQueue<i32>,
    }

    impl SetConsumer {
        fn new(in1: SetInputQueue<i32>) -> Self {
            Self { in1 }
        }
        fn run(&mut self) {
            loop {
                SetConsumer::run_impl(&mut self.in1);
            }
        }

        fn run_impl(in1: &mut SetInputQueue<i32>) {
            println!("In1 {}", in1.next());
        }
    }

    #[test]
    fn cross_beam_test_set() {
        let producer = SetProducer::new();
        let mut consumer = SetConsumer::new(producer.out1.new_consumer());
        scope(|scope| {
            scope.spawn(|_| {
                producer.run();
            });
            consumer.run();
        })
        .unwrap();
    }
}
