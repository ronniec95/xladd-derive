use crate::{
    discovery_service, tcp_listener::TcpTransportListener, tcp_sender::TcpTransportSender,
};
use async_std::sync::{Arc, Mutex};
use futures::{
    channel::mpsc,
    executor::{block_on, ThreadPool},
    task::SpawnExt,
};
use log::*;
use multimap::MultiMap;
use std::net::SocketAddr;
use std::str::FromStr;
// Every input queue needs to implement this
pub trait PushByteData: Send {
    fn push_data(&mut self, v: &[u8]);
}

pub struct QueueManager {
    sender: TcpTransportSender,
    pool: ThreadPool,
}

impl QueueManager {
    pub fn new() -> Self {
        Self {
            sender: TcpTransportSender::new(),
            pool: ThreadPool::new().unwrap(),
        }
    }

    pub fn sender(&self) -> mpsc::Sender<(&'static str, Vec<u8>)> {
        self.sender.new_sender()
    }

    pub fn listener(&self) -> TcpTransportListener {
        TcpTransportListener::new()
    }

    pub fn run_service<Fut>(&self, fut: Fut)
    where
        Fut: futures::Future<Output = ()> + Send + 'static,
    {
        match self.pool.spawn(fut) {
            Ok(_) => (),
            Err(e) => error!("Future failed {}", e),
        }
    }

    pub fn start(self, mut listener: TcpTransportListener) {
        block_on({
            let sub_pool = self.pool.clone();
            async move {
                let (port_sender, port_receiver) = TcpTransportListener::port_channel();
                let (channel_sender, channel_receiver) = TcpTransportSender::channels_channel();
                let discovery_client = discovery_service::run_client(
                    SocketAddr::from_str("127.0.0.1:9999").unwrap(),
                    &[],
                    channel_sender,
                    port_sender,
                );

                sub_pool
                    .spawn({
                        let tcp_senders = Arc::new(Mutex::new(MultiMap::new()));
                        let channel_pool = sub_pool.clone();
                        async move {
                            debug!("Running receive_channel_updates");
                            match &self
                                .sender
                                .receive_channel_updates(
                                    channel_pool,
                                    tcp_senders,
                                    channel_receiver,
                                )
                                .await
                            {
                                Ok(_) => (),
                                Err(e) => error!("Failed while receiving channel updates {:?}", e),
                            }
                        }
                    })
                    .unwrap();

                sub_pool
                    .spawn(async move {
                        println!("Spawning discovery client");
                        match discovery_client.await {
                            Ok(_) => (),
                            Err(e) => {
                                error!("Failed while listeing to the discovery client {:?}", e)
                            }
                        }
                    })
                    .unwrap();

                println!("Listending to port updates");
                listener.listen_port_updates(port_receiver).await.unwrap();
            }
        });
    }
}
/*
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
        let queue = BurstQueue::<i32>::new("hello", "myservice");
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
        let queue = LastValueQueue::<i32>::new("channel1", "myservice");
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
                q1: BurstQueue::<i32>::new("channel1", "myservice"),
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
                q1: oq.burst_pull_queue("channel1").clone(),
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
                q1: oq.burst_pull_queue("channel1").clone(),
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
// DeltaHashMap
// Testing
// Packaging
// Docs
*/
