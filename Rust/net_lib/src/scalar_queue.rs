use crate::queues::PushByteData;
use crate::smart_monitor::{smlogger, SMLogger};
use async_std::sync::{Arc, Mutex};
use futures::{channel::mpsc, Future};
use serde::{Deserialize, Serialize};
use std::borrow::BorrowMut;
use std::pin::Pin;
use std::task::{Context, Poll};

/// A scalar output queue pushes a single item of T to it's one or more receivers
pub struct ScalarOutputQueue<T: Clone + Serialize + Send + 'static> {
    last_value_consumers: Vec<LastValueScalarInputQueue<T>>,
    burst_consumers: Vec<BurstScalarInputQueue<T>>,
    service: &'static str,
}

/// A TCP scalar output queue pushes a single item of T to it's one or more receivers
/// The dicovery service will notify this service of what it's potential queue routes are
/// If you have a tcp queue, it cannot be used as an internal queue at the same time currently,
/// as it's determined at compile time
pub struct TcpScalarOutputQueue<T>
where
    T: Serialize + Send + 'static,
{
    /// Name of the channel
    pub channel_id: &'static str,
    sender: mpsc::Sender<(&'static str, Vec<u8>)>,
    sm_log: SMLogger<T>,
}

/// A Burst Scalar Input queue receives N messages at a time to process as a chunk of work
/// For example if the ScalarOutputQueue<i32> pushes 10 i32s very quickly (depends on the polling interval) they will
/// end up as a Vec<i32>s passed into the receiver
#[derive(Clone)]
pub struct BurstScalarInputQueue<T>
where
    T: Serialize,
{
    /// Name of the channel
    pub channel_id: &'static str,
    values: Arc<Mutex<Vec<T>>>,
    sm_log: SMLogger<T>,
}

/// A Last Value Scalar Input queue receives disregards all messages except the last one
/// For example if the ScalarOutputQueue<i32> pushes 10 i32s very quickly the first 9 will be disregarded and the receiver
/// will get the latest one. Depending on the poll rate, that could be all 10 messages or just the last one, it's not guaranteed
/// But what is guaranteed is that at the time of polling, it's the latest one within the system
#[derive(Clone)]
pub struct LastValueScalarInputQueue<T>
where
    T: Serialize,
{
    /// Name of the channel
    pub channel_id: &'static str,
    value: Arc<Mutex<Option<T>>>,
    sm_log: SMLogger<T>,
}

impl<T> TcpScalarOutputQueue<T>
where
    T: Serialize + Send + 'static,
{
    /// Instantiate a new TCPScalarOutputQueue
    /// * channel_id - Channel name
    /// * service - Service name that this queue belongs to
    /// * sender - A mpsc::Sender to move data around as opaque bytes
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

    /// Send a value T via tcp to it's receivers
    /// The smart monitoring will log the entry and exit of the message from this service
    /// The discovery service will notify of it's receivers
    pub fn send(&mut self, item: T) {
        self.sm_log.entry(&item);
        let bytes = bincode::serialize(&item).unwrap();
        self.sender.try_send((self.channel_id, bytes)).unwrap();
        self.sm_log.exit();
    }
}

// Implmentation of Output queue
// Split this into type specific queues
impl<T> ScalarOutputQueue<T>
where
    T: Clone + Serialize + Send + for<'de> Deserialize<'de> + std::fmt::Debug + 'static,
{
    /// Instantiate a new ScalarOutputQueue
    /// Internal queue between two functions, connected via an asynchronous future
    /// * service - Service name that this queue belongs to
    pub fn new(service: &'static str) -> Self {
        Self {
            last_value_consumers: Vec::new(),
            burst_consumers: Vec::new(),
            service: service,
        }
    }

    /// Last value input queue consumes messages placed on this output queue
    /// You can have multiple consumers of these
    /// * channel_id - Channel name
    pub fn lv_input_queue(
        &'static mut self,
        channel_id: &'static str,
    ) -> &LastValueScalarInputQueue<T> {
        self.last_value_consumers
            .push(LastValueScalarInputQueue::new(channel_id, &self.service));
        self.last_value_consumers.last().unwrap()
    }

    pub fn burst_input_queue(&mut self, channel_id: &'static str) -> &BurstScalarInputQueue<T> {
        self.burst_consumers
            .push(BurstScalarInputQueue::new(channel_id, &*self.service));
        self.burst_consumers.last().unwrap()
    }

    pub fn send(&mut self, item: T) {
        for output in self.last_value_consumers.iter_mut() {
            output.push(item.clone());
        }
        for output in self.burst_consumers.iter_mut() {
            output.push(item.clone());
        }
    }
}

// Implementation of input queues
impl<T> BurstScalarInputQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de>,
{
    fn new(channel_id: &'static str, service: &'static str) -> Self {
        Self {
            channel_id,
            values: Arc::new(Mutex::new(Vec::new())),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    fn push(&mut self, item: T) {
        self.sm_log.entry(&item);
        let mut data = self.values.try_lock().unwrap();
        data.push(item);
    }
}

impl<T> Future for BurstScalarInputQueue<T>
where
    T: Serialize + Unpin + Clone,
{
    type Output = Vec<T>;
    fn poll(mut self: Pin<&mut Self>, ctx: &mut Context) -> Poll<Self::Output> {
        let res = if let Some(mut data) = self.values.try_lock() {
            if !data.is_empty() {
                Some(data.drain(..).collect())
            } else {
                None
            }
        } else {
            None
        };

        match res {
            Some(res) => {
                self.borrow_mut().sm_log.exit();
                Poll::Ready(res)
            }
            None => {
                ctx.waker().wake_by_ref();
                Poll::Pending
            }
        }
    }
}

// Implementation of input queues
impl<T> LastValueScalarInputQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug,
{
    pub fn new(channel_id: &'static str, service: &'static str) -> Self {
        Self {
            channel_id,
            value: Arc::new(Mutex::new(None)),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    fn push(&mut self, item: T) {
        self.sm_log.entry(&item);
        let mut data = self.value.try_lock().unwrap();
        *data = Some(item);
    }
}

impl<T> Future for LastValueScalarInputQueue<T>
where
    T: Serialize + std::marker::Unpin + for<'de> Deserialize<'de> + Copy,
{
    type Output = T;
    fn poll(mut self: Pin<&mut Self>, ctx: &mut Context) -> Poll<Self::Output> {
        if let Some(value) = { self.value.try_lock().map_or_else(|| None, |v| v.or(None)) } {
            self.borrow_mut().sm_log.exit();
            Poll::Ready(value)
        } else {
            ctx.waker().wake_by_ref();
            Poll::Pending
        }
    }
}

impl<T> PushByteData for BurstScalarInputQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug + Send + Sync,
{
    fn push_data(&mut self, v: &[u8]) {
        let v = bincode::deserialize::<T>(v).expect("Could not deserialise from bytes");
        self.push(v);
    }
}

impl<T> PushByteData for LastValueScalarInputQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug + Send + Sync,
{
    fn push_data(&mut self, v: &[u8]) {
        let v = bincode::deserialize::<T>(v).expect("Could not deserialise from bytes");
        self.push(v);
    }
}

#[cfg(test)]
mod test {
    use crate::msg_serde::*;
    use async_std::{
        net::{SocketAddr, TcpListener},
        stream::StreamExt,
    };
    use crossbeam::channel::{unbounded, Receiver, Sender};
    use futures::executor::block_on;
    use log::*;
    use multimap::MultiMap;
    use serde::Deserialize;
    use std::collections::{BTreeMap, BTreeSet};
    use threadpool_crossbeam_channel::Builder;
    pub trait PushByteData {
        fn push_data(&mut self, v: &[u8]);
    }

    struct ScalarOutputQueue<T> {
        producer: Sender<T>,
        consumer: Receiver<T>,
    }

    impl<T> ScalarOutputQueue<T> {
        fn new() -> Self {
            let (producer, consumer) = unbounded();
            Self { producer, consumer }
        }

        fn send(&self, item: T) {
            self.producer.send(item).unwrap();
        }

        fn new_consumer(&self) -> ScalarInputQueue<T> {
            ScalarInputQueue::<T>::new(self.consumer.clone())
        }

        fn new_burst_consumer(&self) -> ScalarBurstInputQueue<T> {
            ScalarBurstInputQueue::<T>::new(self.consumer.clone())
        }
    }

    impl<T> Drop for ScalarOutputQueue<T> {
        fn drop(&mut self) {
            println!("Dropping output queue");
        }
    }
    struct ScalarInputQueue<T> {
        consumer: Receiver<T>,
        last_value: Option<T>,
    }

    impl<T> ScalarInputQueue<T> {
        fn new(consumer: Receiver<T>) -> Self {
            Self {
                consumer,
                last_value: None,
            }
        }

        fn next(&mut self) -> &T {
            if let Ok(v) = self.consumer.recv() {
                self.last_value = Some(v);
            }
            self.last_value.as_ref().unwrap()
        }
    }

    struct ScalarBurstInputQueue<T> {
        consumer: Receiver<T>,
        last_value: Vec<T>,
    }

    impl<T> ScalarBurstInputQueue<T> {
        fn new(consumer: Receiver<T>) -> Self {
            Self {
                consumer,
                last_value: vec![],
            }
        }

        fn next(&mut self) -> Vec<T> {
            while let Ok(v) = self.consumer.recv() {
                self.last_value.push(v);
                if self.consumer.is_empty() {
                    break;
                }
            }
            self.last_value.drain(..).collect()
        }
    }

    struct Producer {
        out1: ScalarOutputQueue<i64>,
        out2: ScalarOutputQueue<i64>,
    }

    impl Producer {
        fn new() -> Self {
            Self {
                out1: ScalarOutputQueue::new(),
                out2: ScalarOutputQueue::new(),
            }
        }

        fn run(&self) {
            for i in 0..10 {
                self.out1.send(i);
                if i % 2 == 0 {
                    self.out2.send(i * 5);
                }
            }
        }
    }

    struct Consumer {
        in1: ScalarInputQueue<i64>,
        in2: ScalarInputQueue<i64>,
    }

    impl Consumer {
        fn new(in1: ScalarInputQueue<i64>, in2: ScalarInputQueue<i64>) -> Self {
            Self { in1, in2 }
        }
        fn run(&mut self) {
            loop {
                Consumer::run_impl(&mut self.in1, &mut self.in2);
            }
        }

        fn run_impl(in1: &mut ScalarInputQueue<i64>, in2: &mut ScalarInputQueue<i64>) {
            println!("In1 {}", in1.next());
            println!("In2 {}", in2.next());
        }
    }

    struct SecondConsumer {
        in1: ScalarInputQueue<i64>,
        in2: ScalarInputQueue<i64>,
    }

    impl SecondConsumer {
        fn new(in1: ScalarInputQueue<i64>, in2: ScalarInputQueue<i64>) -> Self {
            Self { in1, in2 }
        }
        fn run(&mut self) {
            loop {
                SecondConsumer::run_impl(&mut self.in1, &mut self.in2);
            }
        }

        fn run_impl(in1: &mut ScalarInputQueue<i64>, in2: &mut ScalarInputQueue<i64>) {
            println!("2nd In1 {}", in1.next());
            println!("2nd In2 {}", in2.next());
        }
    }

    struct TcpOutputQueue<T> {
        consumer: Receiver<Vec<u8>>,
        consumer_typed: Receiver<T>,
        producer_typed: Sender<T>,
        _phantom: std::marker::PhantomData<T>,
    }

    impl<T> TcpOutputQueue<T>
    where
        T: for<'de> Deserialize<'de>,
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
        fn new_consumer(&self) -> ScalarInputQueue<T> {
            ScalarInputQueue::<T>::new(self.consumer_typed.clone())
        }
    }

    struct TcpConnManager {
        producers: MultiMap<&'static str, Sender<Vec<u8>>>,
    }

    impl TcpConnManager {
        /// Creates a listener on a port
        /// # Arguments
        /// * 'port' - A port number
        ///
        pub fn new() -> Self {
            Self {
                producers: MultiMap::new(),
            }
        }

        pub fn add_producer(&mut self, channel: &'static str, sender: Sender<Vec<u8>>) {
            self.producers.insert(channel, sender);
        }

        pub async fn listen(&self, port: u16) -> Result<(), Box<dyn std::error::Error>> {
            println!("Binding to port {}", port);
            let listener = TcpListener::bind(SocketAddr::from(([0, 0, 0, 0], port))).await?;
            let mut incoming = listener.incoming();
            while let Some(stream) = incoming.next().await {
                let stream = stream?;
                let msg = read_queue_message(stream).await?;
                if let Some(sinks) = self.producers.get_vec(&*msg.channel_name) {
                    for producer in sinks {
                        producer.send(msg.data.clone()).unwrap();
                    }
                }
            }
            Ok(())
        }
    }

    struct TcpProducer {
        out1: TcpOutputQueue<i64>,
        out2: TcpOutputQueue<i64>,
    }

    impl TcpProducer {
        fn new(mgr: &mut TcpConnManager) -> Self {
            Self {
                out1: TcpOutputQueue::new(mgr, "channel1"),
                out2: TcpOutputQueue::new(mgr, "channel2"),
            }
        }

        fn run(self) {
            scope(|scope| {
                scope.spawn(|_| loop {
                    self.out1.send();
                });
                scope.spawn(|_| loop {
                    self.out2.send();
                });
            })
            .unwrap();
        }
    }

    struct VecConsumer {
        in1: ScalarBurstInputQueue<i64>,
        in2: ScalarBurstInputQueue<i64>,
    }

    impl VecConsumer {
        fn new(in1: ScalarBurstInputQueue<i64>, in2: ScalarBurstInputQueue<i64>) -> Self {
            Self { in1, in2 }
        }
        fn run(&mut self) {
            std::thread::sleep(std::time::Duration::from_millis(2000));
            loop {
                VecConsumer::run_impl(&mut self.in1, &mut self.in2);
            }
        }

        fn run_impl(in1: &mut ScalarBurstInputQueue<i64>, in2: &mut ScalarBurstInputQueue<i64>) {
            println!("In1 {:?}", in1.next());
            println!("In2 {:?}", in2.next());
        }
    }

    struct MultiProducer {
        out1: ScalarOutputQueue<i64>,
        out2: ScalarOutputQueue<i64>,
    }

    impl MultiProducer {
        fn new() -> Self {
            Self {
                out1: ScalarOutputQueue::new(),
                out2: ScalarOutputQueue::new(),
            }
        }

        fn run(&self) {
            for i in 0..10 {
                self.out1.send(i);
                if i % 2 == 0 {
                    self.out2.send(i * 5);
                }
            }
            std::thread::sleep(std::time::Duration::from_millis(5000));
            for i in 12..18 {
                self.out1.send(i);
                if i % 2 == 0 {
                    self.out2.send(i * 3);
                }
            }
        }
    }

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

    use crossbeam::thread::scope;
    #[test]
    fn cross_beam_scalar() {
        let producer = Producer::new();
        let mut consumer =
            Consumer::new(producer.out1.new_consumer(), producer.out2.new_consumer());
        scope(|scope| {
            scope.spawn(|_| {
                producer.run();
            });
            consumer.run();
        })
        .unwrap();
    }

    #[test]
    fn cross_beam_multi_consumer() {
        let producer = Producer::new();
        let mut consumer =
            Consumer::new(producer.out1.new_consumer(), producer.out2.new_consumer());

        let mut second_consumer =
            SecondConsumer::new(producer.out1.new_consumer(), producer.out2.new_consumer());
        let threadpool = Builder::new().build();
        threadpool.execute(move || {
            producer.run();
        });
        threadpool.execute(move || {
            consumer.run();
        });
        threadpool.execute(move || {
            second_consumer.run();
        });
        threadpool.join();
    }

    #[test]
    fn cross_beam_test_burst() {
        let producer = Producer::new();
        let mut consumer = VecConsumer::new(
            producer.out1.new_burst_consumer(),
            producer.out2.new_burst_consumer(),
        );
        scope(|scope| {
            scope.spawn(|_| {
                producer.run();
            });
            consumer.run();
        })
        .unwrap();
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

    #[test]
    fn cross_beam_test_burst_multiple() {
        let producer = MultiProducer::new();
        let mut consumer = VecConsumer::new(
            producer.out1.new_burst_consumer(),
            producer.out2.new_burst_consumer(),
        );
        scope(|scope| {
            scope.spawn(|_| {
                producer.run();
            });
            consumer.run();
        })
        .unwrap();
    }

    #[test]
    fn cross_beam_tcp() {
        let threadpool = Builder::new().build();
        let mut mgr = TcpConnManager::new();
        let producer = TcpProducer::new(&mut mgr);
        let mut consumer =
            Consumer::new(producer.out1.new_consumer(), producer.out2.new_consumer());
        threadpool.execute(move || {
            producer.run();
        });
        threadpool.execute(move || {
            consumer.run();
        });

        block_on(async {
            mgr.listen(12345).await.unwrap();
        });
    }
}
