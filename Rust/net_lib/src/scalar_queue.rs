use crate::smart_monitor::{smlogger, SMLogger};
use crate::tcp_listener::TcpConnManager;
use crate::tcp_sender::TcpSinkManager;
use crossbeam::channel::{unbounded, Receiver, Sender};
use serde::{Deserialize, Serialize};

/// A scalar output queue pushes a single item of T to it's one or more receivers
pub struct ScalarOutputQueue<T> {
    producer: Sender<T>,
    consumer: Receiver<T>,
}

impl<T> ScalarOutputQueue<T> {
    pub fn new() -> Self {
        let (producer, consumer) = unbounded();
        Self { producer, consumer }
    }

    pub fn send(&self, item: T) {
        self.producer.send(item).unwrap();
    }

    pub fn new_consumer(&self) -> ScalarInputQueue<T> {
        ScalarInputQueue::<T>::new(self.consumer.clone())
    }

    pub fn new_burst_consumer(&self) -> ScalarBurstInputQueue<T> {
        ScalarBurstInputQueue::<T>::new(self.consumer.clone())
    }
}

pub struct ScalarInputQueue<T> {
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

    pub fn next(&mut self) -> &T {
        if let Ok(v) = self.consumer.recv() {
            self.last_value = Some(v);
        }
        self.last_value.as_ref().unwrap()
    }
}

/// A Burst Scalar Input queue receives N messages at a time to process as a chunk of work
/// For example if the ScalarOutputQueue<i32> pushes 10 i32s very quickly (depends on the polling interval) they will
/// end up as a Vec<i32>s passed into the receiver
pub struct ScalarBurstInputQueue<T> {
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

/// A TCP scalar output queue connects via the TcpConnManager and listens to messages on it's specified channel
/// Use in a Producer object as a source of data that you pass onto an input queue
pub struct TcpOutputQueue<T> {
    consumer: Receiver<Vec<u8>>,
    consumer_typed: Receiver<T>,
    producer_typed: Sender<T>,
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

pub struct TcpSinkQueue<T> {
    producer: Sender<(&'static str, Vec<u8>)>,
    channel: &'static str,
    _phantom: std::marker::PhantomData<T>,
}

impl<T> TcpSinkQueue<T>
where
    T: Serialize,
{
    fn new(mgr: &mut TcpSinkManager, channel: &'static str) -> Self {
        Self {
            producer: mgr.new_producer(),
            channel,
            _phantom: std::marker::PhantomData,
        }
    }

    fn send(&self, item: T) {
        let bytes = bincode::serialize(&item).expect("Could not serialise into bytes");
        self.producer.try_send((self.channel, bytes)).unwrap();
    }
}

/*
/// A scalar output queue pushes a single item of T to it's one or more receivers
pub struct ScalarOutputQueue<T: Clone + Serialize + Send + 'static> {
    last_value_consumers: Vec<LastValueScalarInputQueue<T>>,
    burst_consumers: Vec<BurstScalarInputQueue<T>>,
    service: &'static str,
}

pub struct TcpScalarOutputQueue<T>
where
    T: Serialize + Send + 'static,
{
    /// Name of the channel
    pub channel_id: &'static str,
    sender: mpsc::Sender<(&'static str, Vec<u8>)>,
    sm_log: SMLogger<T>,
}

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
*/
#[cfg(test)]
mod test {
    use super::*;
    use futures::executor::block_on;
    use threadpool_crossbeam_channel::Builder;

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

    struct ConsumerChainA {
        in1: ScalarInputQueue<i64>,
        out1: ScalarOutputQueue<i64>,
    }

    impl ConsumerChainA {
        fn new(in1: ScalarInputQueue<i64>) -> Self {
            Self {
                in1,
                out1: ScalarOutputQueue::<i64>::new(),
            }
        }
        fn run(&mut self) {
            loop {
                let res = ConsumerChainA::run_impl(&mut self.in1);
                self.out1.send(res);
                // For the purposes of testing only as print overwhelms display
                std::thread::sleep(std::time::Duration::from_millis(100));
            }
        }

        fn run_impl(in1: &mut ScalarInputQueue<i64>) -> i64 {
            let v = in1.next();
            println!("Consumer Chain A == In1 {}", v);
            v + 5
        }
    }

    struct ConsumerChainB {
        in1: ScalarInputQueue<i64>,
    }

    impl ConsumerChainB {
        fn new(in1: ScalarInputQueue<i64>) -> Self {
            Self { in1 }
        }
        fn run(&mut self) {
            loop {
                ConsumerChainB::run_impl(&mut self.in1);
            }
        }

        fn run_impl(in1: &mut ScalarInputQueue<i64>) {
            println!("Consumer Chain B == In1 {}", in1.next());
        }
    }

    struct TcpConsumerChain {
        in1: ScalarInputQueue<i64>,
        out: TcpSinkQueue<i64>,
    }

    impl TcpConsumerChain {
        fn new(
            mgr: &mut TcpSinkManager,
            channel: &'static str,
            in1: ScalarInputQueue<i64>,
        ) -> Self {
            Self {
                in1,
                out: TcpSinkQueue::<i64>::new(mgr, channel),
            }
        }
        fn run(&mut self) {
            loop {
                let res = TcpConsumerChain::run_impl(&mut self.in1);
                self.out.send(res);
            }
        }

        fn run_impl(in1: &mut ScalarInputQueue<i64>) -> i64 {
            let v = in1.next();
            println!("Tcp Chain B == In1 {}", v);
            v * 3
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
    fn cross_beam_chain() {
        let producer = Producer::new();
        let mut consumer = ConsumerChainA::new(producer.out1.new_consumer());

        let mut second_consumer = ConsumerChainB::new(consumer.out1.new_consumer());
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
    fn cross_beam_tcp_chain() {
        let mut mgr = TcpSinkManager::new();

        let producer = Producer::new();
        let mut consumer = ConsumerChainA::new(producer.out1.new_consumer());

        let mut second_consumer =
            TcpConsumerChain::new(&mut mgr, "tcp_channel", consumer.out1.new_consumer());
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
        mgr.run();
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
