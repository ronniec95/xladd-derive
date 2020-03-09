use futures::executor::{block_on, ThreadPool};
use futures::task::SpawnExt;
use log::*;
use net_lib::discovery_service;
use net_lib::msg_serde::Channel;
use net_lib::queues::{LastValueQueue, OutputQueue, TcpQueueManager};
use std::borrow::Cow;
use std::net::SocketAddr;
use std::str::FromStr;

struct MainService {
    pub q0: LastValueQueue<i64>,
    pub q1: OutputQueue<i64>,
}

impl MainService {
    fn new(tcp_queue_mgr: &TcpQueueManager) -> Self {
        let init = Self {
            q0: LastValueQueue::new("inputchannel".to_string(), Cow::Borrowed("mainservice")), // Tcp input queue
            q1: OutputQueue::new("output_channel"), // Multiple outputs
        };
        init
    }
    async fn run(&mut self) {
        let value = self.q0.clone().await;
        eprintln!("{}", value);
        self.q1.send(45);
    }
}

fn update(channels: &[Channel]) {
    info!("Current channel map {:?}", channels);
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let pool = ThreadPool::new().unwrap();
    let discovery_client = discovery_service::run_client(
        SocketAddr::from_str("127.0.0.1:9999").unwrap(),
        &[],
        &update,
    );
    block_on(async move {
        discovery_client.await;
    });
    Ok(())
}
