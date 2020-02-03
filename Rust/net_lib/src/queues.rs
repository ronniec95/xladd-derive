use crate::error::*;
use crate::smart_monitor::*;
use async_std::net::{TcpListener, TcpStream};
use async_std::stream::StreamExt;
use futures::channel::mpsc::{channel, Receiver, Sender};
use futures::executor::block_on;
use futures::io::{AsyncReadExt, AsyncWriteExt};
use futures::lock::Mutex;
use futures::Future;
use serde::{Deserialize, Serialize};
use std::borrow::Borrow;
use std::borrow::BorrowMut;
use std::clone::Clone;
use std::collections::{BTreeMap, BTreeSet};
use std::net::SocketAddr;
use std::pin::Pin;
use std::sync::Arc;
use std::task::{Context, Poll};
type ChannelID = u32;

thread_local! {static LOGGER: SmartMonitorClient = SmartMonitorClient::new() }

fn logger() -> &'static std::thread::LocalKey<SmartMonitorClient> {
    return LOGGER.borrow();
}

enum MsgType<T> {
    Update(SocketAddr),
    UpdateCluster(Vec<SocketAddr>),
    Send(T),
}

#[derive(Clone)]
struct BurstQueue<T>
where
    T: Serialize,
{
    values: Arc<Mutex<Vec<T>>>,
    sm_log: SMLogger<T>,
}

#[derive(Clone)]
struct LastValueQueue<T>
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

struct TcpScalarQueue<T>
where
    T: Serialize + Send + 'static,
{
    sender: Sender<MsgType<T>>,
    sm_log: SMLogger<T>,
}

struct PushQueue<T: Clone + Serialize + Send + 'static> {
    last_value_consumers: Vec<LastValueQueue<T>>,
    burst_consumers: Vec<BurstQueue<T>>,
    sink_consumers: Vec<TcpScalarQueue<T>>,
}

impl<T> PushQueue<T>
where
    T: Clone + Serialize + Send + for<'de> Deserialize<'de> + std::fmt::Debug + 'static,
{
    fn new() -> Self {
        Self {
            last_value_consumers: Vec::new(),
            burst_consumers: Vec::new(),
            sink_consumers: Vec::new(),
        }
    }

    pub fn lv_pull_queue(&mut self) -> &LastValueQueue<T> {
        self.last_value_consumers.push(LastValueQueue::new());
        self.last_value_consumers.last().unwrap()
    }

    pub fn burst_pull_queue(&mut self) -> &BurstQueue<T> {
        self.burst_consumers.push(BurstQueue::new());
        self.burst_consumers.last().unwrap()
    }

    pub fn tcp_scalar_pull_queue(&mut self, channel_id: ChannelID) -> &TcpScalarQueue<T> {
        let (sender, receiver) = channel(10);
        std::thread::spawn(move || PushQueue::tcp_stream_sender(channel_id, receiver));
        self.sink_consumers.push(TcpScalarQueue {
            sender,
            sm_log: logger().with(|logger| logger.create_sender::<T>()),
        });
        self.sink_consumers.last().unwrap()
    }

    async fn tcp_stream_sender(
        channel_id: ChannelID,
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
                                    tcp_stream.write(&u32::to_be_bytes(channel_id)).await?;
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

    fn send(&mut self, item: T) {
        for output in self.last_value_consumers.iter_mut() {
            output.push(item.clone());
        }
        for output in self.burst_consumers.iter_mut() {
            dbg!(&item);
            output.push(item.clone());
        }
        for output in self.sink_consumers.iter_mut() {
            output.push(item.clone());
        }
    }
}

impl<T> BurstQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de>,
{
    fn new() -> Self {
        Self {
            values: Arc::new(Mutex::new(Vec::new())),
            sm_log: logger().with(|logger| logger.create_sender::<T>()),
        }
    }

    fn push(&mut self, item: T) {
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
        if let Some(res) = if let Some(mut data) = self.values.try_lock() {
            if !data.is_empty() {
                Some(data.drain(..).collect())
            } else {
                None
            }
        } else {
            None
        } {
            Poll::Ready(res)
        } else {
            ctx.waker().wake_by_ref();
            Poll::Pending
        }
    }
}

impl<T> LastValueQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug,
{
    fn new() -> Self {
        Self {
            value: Arc::new(Mutex::new(None)),
            sm_log: logger().with(|logger| logger.create_sender::<T>()),
        }
    }

    fn push_bytes(&mut self, data: &[u8]) {
        let v = bincode::deserialize::<T>(data).expect("Could not deserialise from bytes");
        self.push(v);
    }

    fn push(&mut self, item: T) {
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
            self.borrow_mut().sm_log.entry(&value);
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
        self.sender.try_send(MsgType::Send(item)).unwrap();
    }

    pub fn update_socket(&mut self, addr: SocketAddr) {
        self.sender.try_send(MsgType::Update(addr)).unwrap();
    }
}

pub trait ByteDeSerialiser {
    fn push_data(&mut self, v: &[u8]);
}

impl<T> ByteDeSerialiser for BurstQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug + Send,
{
    fn push_data(&mut self, v: &[u8]) {
        self.push_bytes(&v);
    }
}

impl<T> ByteDeSerialiser for LastValueQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug + Send,
{
    fn push_data(&mut self, v: &[u8]) {
        self.push_bytes(&v);
    }
}

/// Implements a tcp external connection for channels
///
pub struct TcpService {
    port: u16,
    input_queue_map: BTreeMap<ChannelID, Vec<Box<dyn ByteDeSerialiser>>>,
}

impl TcpService {
    /// Creates a listener on a port
    /// # Arguments
    /// * 'port' - A port number
    ///
    pub fn new_listener(port: u16) -> Self {
        Self {
            port,
            input_queue_map: BTreeMap::new(),
        }
    }

    /// Updates the port after receiving an update from the discovery service
    /// # Arguments
    /// * 'port' - A port number
    pub fn update_port(&mut self, port: u16) {
        self.port = port;
    }

    /// Register a queue that's going to listen on a tcp port
    /// # Arguments
    /// * 'id' - channel id
    /// * 'sink' - queue that implments byteDeserialiser
    ///
    pub fn register_input_queue(&mut self, id: ChannelID, sink: Box<dyn ByteDeSerialiser>) {
        self.input_queue_map.entry(id).or_insert(vec![]).push(sink);
    }

    pub fn unregister_input_queue(&mut self, id: &ChannelID) {
        self.input_queue_map.remove(&id);
    }

    pub async fn listen(&mut self) -> Result<(), MeshError> {
        let listener = block_on(async {
            TcpListener::bind(SocketAddr::from(([0, 0, 0, 0], self.port)))
                .await
                .expect("Could not bind to socket")
        });
        let mut incoming = listener.incoming();
        while let Some(stream) = incoming.next().await {
            let reader = &mut &stream?;
            let mut channel_id = [0u8; 4];
            let mut byte_len = [0u8; 4];
            reader.read_exact(&mut channel_id).await?;
            reader.read_exact(&mut byte_len).await?;
            let channel_id = u32::from_be_bytes(channel_id);
            let mut buffer = Vec::with_capacity(u32::from_be_bytes(byte_len) as usize);
            reader.read_exact(&mut buffer).await?;
            if let Some(sinks) = self.input_queue_map.get_mut(&channel_id) {
                for sink in sinks {
                    sink.push_data(&buffer)
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
    use futures::executor::{block_on, LocalPool};
    use futures::task::SpawnExt;
    use std::time::Duration;

    #[test]
    fn fifo_queue() {
        let mut pool = LocalPool::new();
        let queue = BurstQueue::<i32>::new();
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
        let queue = LastValueQueue::<i32>::new();
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
                q1: BurstQueue::<i32>::new(),
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
        oq: PushQueue<i32>,
    }

    impl SourceNode {
        fn new() -> Self {
            Self {
                oq: PushQueue::new(),
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
        fn new(oq: &mut PushQueue<i32>) -> Self {
            Self {
                q1: oq.burst_pull_queue().clone(),
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
        fn new(oq: &mut PushQueue<i32>) -> Self {
            Self {
                q1: oq.burst_pull_queue().clone(),
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
}
