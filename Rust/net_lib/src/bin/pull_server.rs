use async_std::sync::{Arc, Mutex};
use futures::executor::{block_on, ThreadPool};
use futures::task::SpawnExt;
use log::*;
use multimap::MultiMap;
use net_lib::discovery_service;
use net_lib::queues::{LastValueQueue, OutputQueue, TcpTransportListener, TcpTransportSender};
use simplelog::*;
use std::borrow::Cow;
use std::net::SocketAddr;
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
    let mut ls = TcpTransportSender::new();

    block_on({
        let mut ms = TcpTransportListener::new();
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
                .spawn({
                    let tcp_senders = Arc::new(Mutex::new(MultiMap::new()));
                    async move {
                        debug!("Running message multiplexer");
                        ls.run_message_multiplexer(tcp_senders).await;
                    }
                })
                .unwrap();
            sub_pool
                .spawn({
                    let tcp_senders = Arc::new(Mutex::new(MultiMap::new()));
                    let channel_pool = sub_pool.clone();
                    async move {
                        debug!("Running receive_channel_updates");
                        match TcpTransportSender::receive_channel_updates(
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
                        Err(e) => error!("Failed while listeing to the discovery client {:?}", e),
                    }
                })
                .unwrap();

            println!("Listending to port updates");
            ms.listen_port_updates(port_receiver).await.unwrap();
        }
    });
    Ok(())
}
