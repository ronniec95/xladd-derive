use crate::error::MeshError;
use async_std::net::{TcpListener, TcpStream};
use async_std::prelude::*;
use async_std::task;
use byteorder::{BigEndian, ByteOrder};
use futures::channel::mpsc::{channel, Receiver, Sender};
use futures::executor::{block_on, ThreadPool};
use futures::future::join_all;
use futures::task::SpawnExt;
use serde::{Deserialize, Serialize};
use std::cell::RefCell;
use std::collections::{BTreeMap, BTreeSet};
use std::marker::PhantomData;
use std::net::SocketAddr;
use std::sync::Arc;
use std::time::Duration;
// TODO
// Distributer - scatter/gather -TODO
// Span based logging - TODO
// Discovery Service - TODO
// Abstract away the transport mechanism - Maybe?

type ChannelID = u32;

pub struct Queue<T> {
    pull_byte_receiver: Receiver<Vec<u8>>,
    push_byte_sender: Sender<Vec<u8>>,
    phantom: PhantomData<T>,
}

pub struct ConstQueue<T> {
    value: fn() -> T,
}

pub struct TimerQueue<T> {
    timer: fn() -> T,
    delay: Duration,
    byte_sender: Sender<Vec<u8>>,
}

enum SendMode {
    Single,
    Burst(usize),
}

pub struct TcpService {
    listener: TcpListener,
    instance: usize,
    pub pool: ThreadPool,
    queues: RefCell<BTreeMap<ChannelID, Vec<Sender<Vec<u8>>>>>,
    queue_map: Arc<BTreeMap<ChannelID, Vec<TcpStream>>>,
    output_queue_map: BTreeMap<ChannelID, Receiver<Vec<u8>>>,
}

impl TcpService {
    pub fn new_listener(port: u16, instance: usize) -> Self {
        let listener = block_on(async {
            TcpListener::bind(SocketAddr::from(([127, 0, 0, 1], port)))
                .await
                .expect("Could not bind to socket")
        });
        Self {
            listener,
            instance,
            pool: ThreadPool::new().unwrap(),
            queues: RefCell::new(BTreeMap::new()),
            queue_map: Arc::new(BTreeMap::new()),
            output_queue_map: BTreeMap::new(),
        }
    }

    pub async fn listen(&mut self) -> Result<(), MeshError> {
        let mut input_queue_list = BTreeSet::new();
        self.queues.borrow().iter().for_each(|(&k, _)| {
            input_queue_list.insert(k);
        });
        let mut incoming = self.listener.incoming();
        while let Some(stream) = incoming.next().await {
            let reader = &mut &stream?;
            let mut channel_id = [0u8; 4];
            let mut byte_len = [0u8; 4];
            reader.read_exact(&mut channel_id).await?;
            reader.read_exact(&mut byte_len).await?;
            let channel_id = BigEndian::read_u32(&channel_id) as u32;
            if channel_id == 0 {
                // Send cpu information
                let mut buffer = [0u8; 8];
                BigEndian::write_u64(&mut buffer, self.instance as u64);
                reader.write(&buffer).await?;
                BigEndian::write_u64(&mut buffer, input_queue_list.len() as u64);
                reader.write(&buffer).await?;
                // Send list of input_queues
                for v in input_queue_list.iter() {
                    BigEndian::write_u32(&mut buffer, *v as u32);
                    reader.write(&buffer).await?;
                }
            }
            let mut buffer = Vec::with_capacity(BigEndian::read_u32(&byte_len) as usize);
            reader.read_exact(&mut buffer).await?;
            if let Some(senders) = self.queues.borrow_mut().get_mut(&channel_id) {
                senders.iter_mut().for_each(|sender| {
                    sender
                        .try_send(buffer.clone())
                        .expect("Failed to send message");
                })
            }
        }
        Ok(())
    }

    pub fn local_addr(&self) -> Result<SocketAddr, MeshError> {
        Ok(self.listener.local_addr()?)
    }

    pub fn create_queue<T>(&mut self, channel_id: ChannelID, queue_size: usize) -> Queue<T>
    where
        T: Serialize + for<'de> Deserialize<'de>,
    {
        let (byte_sender, pull_byte_receiver) = channel(queue_size);
        self.queues
            .borrow_mut()
            .entry(channel_id)
            .and_modify(|queues| queues.push(byte_sender.clone()))
            .or_insert_with(|| vec![byte_sender]);

        let (push_byte_sender, byte_receiver) = channel(0);
        if self
            .output_queue_map
            .insert(channel_id, byte_receiver)
            .is_none()
        {
            self.create_push_queue_2(channel_id);
        }
        Queue {
            pull_byte_receiver,
            push_byte_sender,
            phantom: PhantomData,
        }
    }

    async fn send(
        tcp_stream: &mut TcpStream,
        channel_id: u32,
        bytes: &[u8],
    ) -> Result<(), MeshError> {
        tcp_stream.write(&channel_id.to_le_bytes()).await?;
        tcp_stream.write(bytes).await?;
        Ok(())
    }

    pub fn create_const_node<T>(&self, func: fn() -> T) -> ConstQueue<T> {
        ConstQueue { value: func }
    }

    /*
        pub fn create_push_queue(
            pool: ThreadPool,
            channel_id: u32,
            byte_receiver: &mut Receiver<Vec<u8>>,
            queue_map: &BTreeMap<u32, Vec<Rc<TcpStream>>>,
        ) {
            if let Some(tcp_stream_data) = queue_map.get(&channel_id) {
                let bytes = Arc::new(byte_receiver.try_next().unwrap().expect("No bytes found"));

                for tcp_stream in tcp_stream_data.iter() {
                    let tcp_stream = Rc::get_mut(&mut tcp_stream).unwrap();
                    let bytes = bytes.clone();

                    let fut = async move {
                        TcpService::send(tcp_stream, channel_id, &bytes)
                            .await
                            .expect(&format!(
                                "Failed to connect to {:?}",
                                tcp_stream.peer_addr()
                            ));
                    };
                    let handle = pool.spawn(fut).unwrap();
                }
            }
        }
    */
    pub fn create_push_queue_2(&mut self, channel_id: ChannelID) {
        self.output_queue_map
            .get_mut(&channel_id)
            .and_then(|byte_receiver| byte_receiver.try_next().unwrap())
            .map_or((), |bytes| {
                let queue_map = Arc::get_mut(&mut self.queue_map).unwrap();
                let futures = queue_map
                    .get_mut(&channel_id)
                    .map_or(vec![], |tcp_streams| {
                        tcp_streams
                            .iter_mut()
                            .filter_map(|mut tcp_stream| {
                                let bytes = bytes.clone();
                                Some(async move {
                                    TcpService::send(&mut tcp_stream, channel_id, &bytes)
                                        .await
                                        .expect(&format!(
                                            "Failed to connect to {:?}",
                                            tcp_stream.peer_addr()
                                        ));
                                })
                            })
                            .collect::<Vec<_>>()
                    });
                block_on(async { join_all(futures).await });
            });
    }

    pub fn create_timer_push_queue<T>(
        &mut self,
        f: fn() -> T,
        time: u64,
        channel_id: ChannelID,
    ) -> TimerQueue<T> {
        let (byte_sender, byte_receiver) = channel(0);
        if self
            .output_queue_map
            .insert(channel_id, byte_receiver)
            .is_none()
        {
            self.create_push_queue_2(channel_id);
        }
        TimerQueue {
            timer: f,
            delay: Duration::from_millis(time),
            byte_sender,
        }
    }

    pub fn create_timer_pull_queue<T>(f: fn() -> T, time: u64) -> TimerQueue<T> {
        let (byte_sender, _) = channel(0);
        TimerQueue {
            timer: f,
            delay: Duration::from_millis(time),
            byte_sender,
        }
    }

    pub fn connect(&mut self, channel_id: ChannelID, addr: SocketAddr) {
        Arc::get_mut(&mut self.queue_map)
            .unwrap()
            .entry(channel_id)
            .and_modify(|v| {
                let conn = block_on(async {
                    TcpStream::connect(addr)
                        .await
                        .expect("Failed to connect to speified ip")
                });
                v.push(conn);
            })
            .or_insert_with(|| {
                let conn = block_on(async {
                    TcpStream::connect(addr)
                        .await
                        .expect("Failed to connect to speified ip")
                });
                vec![conn]
            });
    }
}

impl<T> Queue<T>
where
    T: Serialize + for<'de> Deserialize<'de>,
{
    pub async fn pull(&mut self) -> Result<T, MeshError> {
        let bytes = self
            .pull_byte_receiver
            .try_next()?
            .ok_or_else(|| MeshError::ChannelError)?;
        Ok(bincode::deserialize::<T>(&bytes)?)
    }

    pub async fn push(&mut self, item: T) -> Result<(), MeshError> {
        let result = bincode::serialize::<T>(&item)?;
        self.push_byte_sender
            .try_send(result)
            .expect("Could not send message");
        Ok(())
    }
}

impl<T> ConstQueue<T> {
    pub async fn pull(&self) -> Result<T, MeshError> {
        Ok((self.value)())
    }
}

impl<T> TimerQueue<T>
where
    T: Serialize,
{
    pub async fn pull(&self) -> Result<T, MeshError> {
        task::sleep(self.delay).await;
        Ok((self.timer)())
    }

    pub async fn push(&mut self, item: T, timeout: u64) -> Result<(), MeshError> {
        task::sleep(Duration::from_millis(timeout)).await;
        let result = bincode::serialize::<T>(&item)?;
        self.byte_sender.try_send(result)?;
        Ok(())
    }
}

struct VectorQueue<T> {
    byte_sender: Sender<Vec<u8>>,
    phantom: PhantomData<T>,
}

impl<T> VectorQueue<T>
where
    T: Serialize,
{
    pub async fn push(&mut self, item: Vec<T>, batch: usize) -> Result<(), MeshError> {
        for chunk in item.as_slice().chunks(batch) {
            let result = bincode::serialize::<[T]>(chunk)?;
            self.byte_sender.try_send(result)?;
        }
        Ok(())
    }
}

struct DictLastValuePullQueue<K, V> {
    pull_byte_receiver: Receiver<Vec<u8>>,
    phantom: PhantomData<(K, V)>,
}

struct DictPushQueue<K, V> {
    values: BTreeMap<K, V>,
    byte_sender: Sender<Vec<u8>>,
}

impl<K, V> DictLastValuePullQueue<K, V>
where
    K: for<'de> Deserialize<'de>,
    V: for<'de> Deserialize<'de>,
{
    pub async fn pull(mut self) -> Result<Vec<(K, V)>, MeshError> {
        let bytes = self
            .pull_byte_receiver
            .try_next()?
            .ok_or_else(|| MeshError::ChannelError)?;
        Ok(bincode::deserialize::<Vec<(K, V)>>(&bytes)?)
    }
}

impl<K, V> DictPushQueue<K, V>
where
    K: Serialize + Ord + Copy,
    V: Serialize + Copy,
{
    pub async fn push(&mut self, key: K, value: V, mode: SendMode) -> Result<(), MeshError> {
        self.values.insert(key, value);
        match mode {
            SendMode::Single => {
                let result = bincode::serialize::<Vec<(K, V)>>(&vec![(key, value)])?;
                self.byte_sender.try_send(result)?;
            }
            SendMode::Burst(batch) => {
                let mut burst_data = Vec::new();
                for (k, v) in self.values.iter() {
                    burst_data.push((*k, *v));
                    if burst_data.len() == batch {
                        let result = bincode::serialize::<Vec<(K, V)>>(&burst_data)?;
                        self.byte_sender.try_send(result)?;
                    }
                }
            }
        }
        Ok(())
    }
}

struct DiscoveryClient {
    q1: Queue<(u16, Vec<ChannelID>)>,
    q2: Queue<BTreeMap<ChannelID, u16>>,
    output_channels: BTreeMap<ChannelID, u16>,
    channel_ids: Vec<ChannelID>,
    port: u16,
}

struct DiscoveryServer {
    q1: Queue<BTreeMap<ChannelID, u32>>,
}

impl DiscoveryClient {
    fn new(tcp_service: &mut TcpService, channel_ids: &[ChannelID]) -> Self {
        Self {
            q1: tcp_service.create_queue(4294967295, 0),
            q2: tcp_service.create_queue(4294967294, 0),
            output_channels: BTreeMap::new(),
            channel_ids: channel_ids.to_vec(),
            port: tcp_service.local_addr().expect("Not yet bound").port(),
        }
    }

    // fn run(mut self, mut pool: ThreadPool) -> Result<(), MeshError> {
    //     loop {
    //         pool.spawn(async {
    //             let (port, channel_ids) = self.get_channel_list();
    //             match self.q1.push((port, channel_ids.to_vec())).await {
    //                 Ok(_) => (),
    //                 Err(_) => {
    //                     eprintln!("Could not connect and send channel data to discovery service")
    //                 }
    //             }
    //             task::sleep(Duration::from_secs(3)).await;
    //         });

    //         pool.spawn(async {
    //             let output_queues = self
    //                 .q2
    //                 .pull()
    //                 .await
    //                 .expect("Could not read from discovery server");
    //             self.update_output_queues(output_queues)
    //         });
    //     }
    //     Ok(())
    // }

    fn get_channel_list(&self) -> (u16, &[ChannelID]) {
        (self.port, &self.channel_ids)
    }

    fn update_output_queues(&mut self, mut queue: BTreeMap<ChannelID, u16>) {
        self.output_channels.append(&mut queue);
    }
}

#[cfg(test)]
mod tests {
    use std::net::SocketAddr;
    use std::net::TcpListener;
    #[test]
    fn ip_addr() {
        let listener = TcpListener::bind(SocketAddr::from(([127, 0, 0, 1], 0)))
            .expect("Could not bind to address");
        println!("{:?}", listener.local_addr());
    }

    use futures::executor::{block_on, ThreadPool};
    use futures::future::join_all;
    use futures::task::SpawnExt;
    use std::pin::*;
    use std::sync::Arc;
    use serde::{Deserialize, Serialize};
    fn foo(b: &[u8]) {
        println!("{:?}", b);
    }
    #[test]
    fn pin_test() {
        let v0 = &[1, 2, 3, 4, 5, 6, 7, 8, 9, 10];
        let pool = ThreadPool::new().unwrap();
        let handles = (0..10)
            .map(|_| {
                pool.spawn_with_handle(async move {
                    foo(&*v0);
                })
                .unwrap()
            })
            .collect::<Vec<_>>();
        join_all(handles);
        //        handles.into_iter().for_each(|handle| block_on(handle));
    }


    use futures::channel::mpsc::{channel, Receiver, Sender};

    enum Msg {
        Register(SocketAddr),
        Unregister(SocketAddr),
        SendScalar(Vec<u8>),
        SendVec(Vec<Vec<u8>>),
        SendMap(Vec<u8>,Vec<Vec<u8>>),
    }

    struct TcpListener1 {
        pool : ThreadPool,
        sender: Sender<Msg>,        
    }

    struct TcpSender {
        receiver: Receiver<Msg>,
    }

    impl TcpListener1 {
        fn new() -> Self {
            let (sender,receiver) = channel(10);
            std::thread::spawn(|| TcpListener1::run_listener(receiver));
            let pool = ThreadPool::new().unwrap();
            Self { pool , sender }
        }

        fn run_listener(mut receiver: Receiver<Msg>) {
            loop{
                let msg = receiver.try_next().unwrap();
            }
        }

        fn register(&self) {
           // self.sender.try_send(Msg::Register(SocketAddr::new(SocketAddr::new(std::net::Ipv4Addr(127,0,0,1),0))));
        }
    }

    impl TcpSender {
    }

    #[test]
    fn sender() {
        
    }
}