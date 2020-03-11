use crate::msg_serde::*;
use crate::smart_monitor::*;
use async_std::net::TcpListener;
use async_std::stream::StreamExt;
use futures::channel::mpsc;
use futures::channel::oneshot;
use futures::lock::Mutex;
use futures::Future;
use log::*;
use serde::{Deserialize, Serialize};
use std::borrow::{BorrowMut, Cow};
use std::clone::Clone;
use std::collections::{BTreeMap, BTreeSet};
use std::net::SocketAddr;
use std::pin::Pin;
use std::sync::Arc;
use std::task::{Context, Poll};

#[derive(Clone)]
pub struct BurstQueue<T>
where
    T: Serialize,
{
    values: Arc<Mutex<Vec<T>>>,
    sm_log: SMLogger<T>,
}

#[derive(Clone)]
pub struct LastValueQueue<T>
where
    T: Serialize,
{
    value: Arc<Mutex<Option<T>>>,
    sm_log: SMLogger<T>,
}

#[derive(Clone)]
struct SetPullQueue<T> {
    values: BTreeSet<T>,
    updated: BTreeSet<T>,
    deleted: BTreeSet<T>,
}

pub struct TcpScalarQueue<T>
where
    T: Serialize + Send + 'static,
{
    msg_format: MsgFormat,
    sender: mpsc::Sender<Vec<u8>>,
    sm_log: SMLogger<T>,
    _data: std::marker::PhantomData<T>,
}

pub struct OutputQueue<T: Clone + Serialize + Send + 'static> {
    last_value_consumers: Vec<LastValueQueue<T>>,
    burst_consumers: Vec<BurstQueue<T>>,
    tcp_consumer: Vec<TcpScalarQueue<T>>,
    service: Cow<'static, str>,
}

impl<T> OutputQueue<T>
where
    T: Clone + Serialize + Send + for<'de> Deserialize<'de> + std::fmt::Debug + 'static,
{
    pub fn new(service: &'static str) -> Self {
        Self {
            last_value_consumers: Vec::new(),
            burst_consumers: Vec::new(),
            tcp_consumer: Vec::new(),
            service: Cow::Borrowed(service),
        }
    }

    pub fn lv_pull_queue(&mut self, channel_id: ChannelId) -> &LastValueQueue<T> {
        self.last_value_consumers
            .push(LastValueQueue::new(channel_id, self.service.clone()));
        self.last_value_consumers.last().unwrap()
    }

    pub fn burst_pull_queue(&mut self, channel_id: ChannelId) -> &BurstQueue<T> {
        self.burst_consumers
            .push(BurstQueue::new(channel_id, self.service.clone()));
        self.burst_consumers.last().unwrap()
    }

    fn tcp_sink(&mut self, channel_id: ChannelId, sender: mpsc::Sender<Vec<u8>>) {
        self.tcp_consumer.push(TcpScalarQueue {
            msg_format: MsgFormat::Bincode,
            sender,
            sm_log: smlogger().create_sender::<T>(channel_id, self.service.clone()),
            _data: std::marker::PhantomData,
        });
    }

    pub fn send(&mut self, item: T) {
        for output in self.last_value_consumers.iter_mut() {
            output.push(item.clone());
        }
        for output in self.burst_consumers.iter_mut() {
            dbg!(&item);
            output.push(item.clone());
        }
        for output in self.tcp_consumer.iter_mut() {
            output.push(item.clone());
        }
    }
}

impl<T> BurstQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de>,
{
    fn new(channel_id: ChannelId, service: Cow<'static, str>) -> Self {
        Self {
            values: Arc::new(Mutex::new(Vec::new())),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    fn push(&mut self, item: T) {
        self.sm_log.entry(&item);
        let mut data = self.values.try_lock().unwrap();
        data.push(item);
    }

    fn push_bytes(&mut self, data: &[u8]) {
        let v = bincode::deserialize::<T>(data).expect("Could not deserialise from bytes");
        self.push(v);
    }
}

impl<T> Future for BurstQueue<T>
where
    T: Serialize + Unpin + Copy + Clone,
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

impl<T> LastValueQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug,
{
    pub fn new(channel_id: ChannelId, service: Cow<'static, str>) -> Self {
        Self {
            value: Arc::new(Mutex::new(None)),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    fn push_bytes(&mut self, data: &[u8]) {
        let v = bincode::deserialize::<T>(data).expect("Could not deserialise from bytes");
        self.push(v);
    }

    fn push(&mut self, item: T) {
        self.sm_log.entry(&item);
        let mut data = self.value.try_lock().unwrap();
        *data = Some(item);
    }
}

impl<T> Future for LastValueQueue<T>
where
    T: Serialize + std::marker::Unpin + Copy + for<'de> Deserialize<'de> + std::fmt::Debug,
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

impl<T> TcpScalarQueue<T>
where
    T: Serialize + Send + 'static,
{
    pub fn push(&mut self, item: T) {
        let bytes = bincode::serialize(&item).unwrap();
        self.sender.try_send(bytes).unwrap();
    }
}
/*
    async fn tcp_stream_sender(
        channel_id: ChannelId,
        mut receiver: Receiver<MsgType<T>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        let mut tcp_stream: Option<TcpStream> = None;
        loop {
            if let Some(msg) = receiver.try_next()? {
                match msg {
                    MsgType::Send(item) => {
                        if let Some(tcp_stream) = &mut tcp_stream {
                            let err = bincode::serialize(&item).and_then(|bytes| {
                                block_on(async {
                                    tcp_stream.write(&usize::to_be_bytes(channel_id)).await?;
                                    tcp_stream.write(&bytes).await?;
                                    Ok(())
                                })
                            });
                            match err {
                                Ok(_) => (),
                                Err(e) => eprintln!("Tcp stream message sending failed {}", e),
                            }
                        }
                    }
                    MsgType::Update(addr) => {
                        if let Some(tcp_stream) = &mut tcp_stream {
                            let err: Result<(), Box<dyn std::error::Error>> = block_on(async {
                                tcp_stream.shutdown(std::net::Shutdown::Both)?;
                                *tcp_stream = TcpStream::connect(addr).await?;
                                Ok(())
                            });
                            match err {
                                Ok(_) => (),
                                Err(e) => eprintln!("Tcp reconnection failed {:?}", e),
                            }
                        }
                    }
                    MsgType::UpdateCluster(addresses) => {}
                }
            }
        }
    }
*/

pub trait ByteDeSerialiser: Send + Sync {
    fn push_data(&mut self, v: &[u8]);
}

impl<T> ByteDeSerialiser for BurstQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug + Send + Sync,
{
    fn push_data(&mut self, v: &[u8]) {
        self.push_bytes(&v);
    }
}

impl<T> ByteDeSerialiser for LastValueQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug + Send + Sync,
{
    fn push_data(&mut self, v: &[u8]) {
        self.push_bytes(&v);
    }
}

struct TcpConnection {}

/// Implements a tcp external connection for channels
///
pub struct TcpTransportListener {
    input_queue_map: BTreeMap<ChannelId, Vec<Box<dyn ByteDeSerialiser>>>,
    output_queue_map: BTreeMap<ChannelId, Vec<Box<dyn ByteDeSerialiser>>>,
    tcp_map: BTreeMap<ChannelId, TcpConnection>,
    channel_receiver: mpsc::Receiver<Vec<Channel>>,
    // Senders
    pub channel_sender: mpsc::Sender<Vec<Channel>>,
}

impl TcpTransportListener {
    /// Creates a listener on a port
    /// # Arguments
    /// * 'port' - A port number
    ///
    pub fn new() -> Self {
        let (channel_sender, channel_receiver) = mpsc::channel(1);
        Self {
            input_queue_map: BTreeMap::new(),
            output_queue_map: BTreeMap::new(),
            tcp_map: BTreeMap::new(),
            channel_receiver,
            channel_sender,
        }
    }

    /// Register a queue that's going to listen on a tcp port
    /// # Arguments
    /// * 'id' - channel id
    /// * 'sink' - queue that implments byteDeserialiser
    ///
    pub fn add_input(&mut self, id: ChannelId, sink: Box<dyn ByteDeSerialiser>) {
        self.input_queue_map.entry(id).or_insert(vec![]).push(sink);
    }

    pub fn add_output<T>(&mut self, id: ChannelId, output_q: &mut OutputQueue<T>)
    where
        T: Serialize + for<'de> Deserialize<'de> + Clone + Send + std::fmt::Debug,
    {
        let (sender, receiver) = mpsc::channel(10);
        output_q.tcp_sink(id, sender);
    }

    pub fn port_channel() -> (oneshot::Sender<u16>, oneshot::Receiver<u16>) {
        oneshot::channel()
    }

    pub async fn listen_port_updates(
        &mut self,
        mut port_receiver: oneshot::Receiver<u16>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        loop {
            if let Ok(port) = port_receiver.try_recv() {
                if let Some(port) = port {
                    debug!("Got a port {}", port);
                    self.listen(port).await?;
                }
            } else {
                async_std::task::sleep(std::time::Duration::from_millis(100)).await;
            }
        }
    }

    async fn listen(&mut self, port: u16) -> Result<(), Box<dyn std::error::Error>> {
        info!("Binding to port {}", port);
        let listener = TcpListener::bind(SocketAddr::from(([0, 0, 0, 0], port))).await?;
        let mut incoming = listener.incoming();
        while let Some(stream) = incoming.next().await {
            let stream = stream?;
            let msg = read_queue_message(stream).await?;
            if let Some(sinks) = self.input_queue_map.get_mut(&msg.channel_name) {
                for sink in sinks {
                    sink.push_data(&msg.data)
                }
            }
        }
        Ok(())
    }
}

// ChannelID/SockAddr update
// Discovery service
// Recovery
// Smart logging
// Distributed data structures
#[cfg(test)]
mod tests {
    use super::*;
    use async_std::task;
    use futures::executor::LocalPool;
    use futures::task::SpawnExt;
    use std::time::Duration;

    #[test]
    fn fifo_queue() {
        let mut pool = LocalPool::new();
        let queue = BurstQueue::<i32>::new(ChannelId::from("hello"), Cow::Borrowed("myservice"));
        let spawner = pool.spawner();
        let mut l_queue = queue.clone();
        spawner
            .spawn(async move {
                loop {
                    let x = queue.clone().await;
                    println!("{:?}", x);
                }
            })
            .unwrap();
        spawner
            .spawn(async move {
                l_queue.push(5);
                l_queue.push(6);
                l_queue.push(7);
                l_queue.push(8);
                task::sleep(Duration::from_secs(5)).await;
                l_queue.push(12);
                l_queue.push(16);
                l_queue.push(17);
                l_queue.push(18);
            })
            .expect("Could not spawn");
        pool.run();
        assert!(true);
    }

    #[test]
    fn last_value_queue() {
        let mut pool = LocalPool::new();
        let queue =
            LastValueQueue::<i32>::new(ChannelId::from("channel1"), Cow::Borrowed("myservice"));
        let spawner = pool.spawner();
        let mut l_queue = queue.clone();
        spawner
            .spawn(async move {
                let mut counter = 10;
                loop {
                    let x = queue.clone().await;
                    println!("{:?}", x);
                    counter = counter - 1;
                    if counter == 0 {
                        break;
                    }
                }
            })
            .unwrap();
        l_queue.push(5);
        l_queue.push(6);
        l_queue.push(7);
        l_queue.push(8);
        pool.run();
        assert!(true);
    }

    struct ProducerService {
        q1: BurstQueue<i32>,
    }

    struct ConsumerService {
        q1: BurstQueue<i32>,
    }

    impl ProducerService {
        fn new() -> Self {
            Self {
                q1: BurstQueue::<i32>::new(ChannelId::from("channel1"), Cow::Borrowed("myservice")),
            }
        }

        async fn run(&mut self) {
            for i in 0..10 {
                self.q1.push(i);
            }
            task::sleep(Duration::from_secs(1)).await;
            for i in 0..10 {
                self.q1.push(i);
            }
        }
    }

    impl ConsumerService {
        fn new(q1: BurstQueue<i32>) -> Self {
            Self { q1 }
        }

        async fn run(&mut self) {
            let mut counter = 0;
            while counter < 2 {
                let value = self.q1.clone().await;
                dbg!("Consuming", &value);
                task::sleep(Duration::from_secs(1)).await;
                counter = counter + 1;
            }
        }
    }

    #[test]
    fn internal_services_x() {
        let mut pool = LocalPool::new();
        let spawner = pool.spawner();

        let mut producer = ProducerService::new();
        let mut consumer = ConsumerService::new(producer.q1.clone());

        spawner.spawn(async move { consumer.run().await }).unwrap();
        spawner.spawn(async move { producer.run().await }).unwrap();
        pool.run();
        assert!(true);
    }

    struct SourceNode {
        oq: OutputQueue<i32>,
    }

    impl SourceNode {
        fn new() -> Self {
            Self {
                oq: OutputQueue::new("service"),
            }
        }

        async fn run(&mut self) {
            for i in 0..10 {
                self.oq.send(i);
            }
            task::sleep(Duration::from_secs(1)).await;
            println!("Producing again");
            for i in 0..10 {
                self.oq.send(i);
            }
        }
    }

    struct ComputeNode {
        q1: BurstQueue<i32>,
    }

    impl ComputeNode {
        fn new(oq: &mut OutputQueue<i32>) -> Self {
            Self {
                q1: oq.burst_pull_queue(ChannelId::from("channel1")).clone(),
            }
        }

        async fn run(&mut self) {
            let mut counter = 0i32;
            while counter < 2 {
                let value = self.q1.clone().await;
                dbg!("Consuming", &value);
                task::sleep(Duration::from_secs(1)).await;
                counter = counter + 1;
            }
        }
    }

    struct ComputeNode2 {
        q1: BurstQueue<i32>,
    }

    impl ComputeNode2 {
        fn new(oq: &mut OutputQueue<i32>) -> Self {
            Self {
                q1: oq.burst_pull_queue(ChannelId::from("channel1")).clone(),
            }
        }

        async fn run(&mut self) {
            let mut counter = 0i32;
            while counter < 2 {
                let value = self.q1.clone().await;
                dbg!("Consuming 2", &value);
                task::sleep(Duration::from_secs(1)).await;
                counter = counter + 1;
            }
        }
    }
    #[test]
    fn multi_consumer_test() {
        let mut pool = LocalPool::new();
        let spawner = pool.spawner();

        let mut source = SourceNode::new();
        let mut sink = ComputeNode::new(&mut source.oq);
        let mut sink2 = ComputeNode2::new(&mut source.oq);
        spawner.spawn(async move { source.run().await }).unwrap();
        spawner.spawn(async move { sink.run().await }).unwrap();
        spawner.spawn(async move { sink2.run().await }).unwrap();
        pool.run();
        assert!(true);
    }

    #[test]
    fn disovery_integration() {}
}
