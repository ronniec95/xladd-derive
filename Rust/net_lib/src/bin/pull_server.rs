use async_std::sync::{Arc, RwLock};
use futures::executor::{block_on, ThreadPool};
use futures::task::SpawnExt;
use log::*;
use net_lib::discovery_service;
use net_lib::msg_serde::Channel;
use net_lib::queues::{LastValueQueue, OutputQueue, TcpTransportListener, TcpTransportSender};
use simplelog::*;
use std::borrow::Cow;
use std::net::SocketAddr;
use std::pin::Pin;
use std::str::FromStr;

struct MainService {
    pub q0: LastValueQueue<i64>,
    pub q1: OutputQueue<i64>,
}

impl MainService {
    fn new(tcp_queue_mgr: &TcpTransportListener) -> Self {
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

fn main() -> Result<(), Box<dyn std::error::Error>> {
    TermLogger::init(
        LevelFilter::Debug,
        ConfigBuilder::new()
            .set_time_level(LevelFilter::Error)
            .set_time_format_str("%Y-%m-%d %H:%M:%S%.3f")
            .build(),
        TerminalMode::Mixed,
    )
    .unwrap();
    let pool = ThreadPool::new().unwrap();

    block_on({
        let mut ms = TcpTransportListener::new();
        let mut ls = TcpTransportSender::new();
        let sub_pool = pool.clone();
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
                .spawn(async move {
                    println!("Spawning discovery client");
                    match discovery_client.await {
                        Ok(_) => (),
                        Err(e) => error!("Failed while listeing to the discovery client {:?}", e),
                    }
                })
                .unwrap();
            sub_pool
                .spawn({
                    let receiver_pool = sub_pool.clone();
                    async move {
                        println!("Listending to channel updates");
                        match ls
                            .receive_channel_updates(receiver_pool, channel_receiver)
                            .await
                        {
                            Ok(_) => (),
                            Err(e) => error!("Failed while receiving channel updates {:?}", e),
                        }
                    }
                })
                .unwrap();
            println!("Listending to port updates");
            ms.listen_port_updates(port_receiver).await.unwrap();
        }
    });
    Ok(())
}
