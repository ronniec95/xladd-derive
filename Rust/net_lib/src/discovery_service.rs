use crate::msg_serde::*;
use async_std::net::SocketAddr;
use async_std::net::{TcpListener, TcpStream};
use async_std::prelude::*;
use async_std::task::sleep;
use chrono::Utc;
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use rand::rngs::SmallRng;
use rand::{Rng, SeedableRng};
use std::collections::BTreeSet;
use urlparse::Url;

pub struct DiscoveryServer {
    stream: TcpStream,
}

impl DiscoveryServer {
    fn new(stream: TcpStream) -> Self {
        Self { stream }
    }

    async fn on_connect(
        &self,
        msg: DiscoveryMessage,
        rng: &mut SmallRng,
    ) -> Result<(), Box<dyn std::error::Error>> {
        match msg.uri.port {
            None => {
                let n = rng.gen_range(1024, std::u16::MAX);
                let new_address = Url::parse(&format!(
                    "{}://{}:{}{}",
                    msg.uri.scheme, msg.uri.netloc, n, msg.uri.path
                ));
                write_msg(
                    self.stream.clone(),
                    DiscoveryMessage {
                        state: DiscoveryState::ConnectResponse,
                        uri: new_address,
                        channels: vec![],
                    },
                )
                .await?;
            }
            Some(_) => {
                write_msg(
                    self.stream.clone(),
                    DiscoveryMessage {
                        state: DiscoveryState::ConnectResponse,
                        uri: msg.uri,
                        channels: vec![],
                    },
                )
                .await?;
            }
        }
        Ok(())
    }

    async fn on_connect_response(
        &self,
        _: DiscoveryMessage,
    ) -> Result<(), Box<dyn std::error::Error>> {
        Ok(())
    }

    async fn on_queue_data(
        &self,
        msg: DiscoveryMessage,
        channels: &mut BTreeSet<Channel>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        msg.channels.iter().for_each(|ch| {
            channels.insert(ch.clone());
        });
        write_msg(
            self.stream.clone(),
            DiscoveryMessage {
                state: DiscoveryState::QueueData,
                uri: msg.uri,
                channels: channels.iter().cloned().collect::<Vec<_>>(),
            },
        )
        .await?;
        Ok(())
    }

    async fn on_error(&self, _: DiscoveryMessage) -> Result<(), Box<dyn std::error::Error>> {
        Ok(())
    }

    async fn run_loop(&self) -> Result<(), Box<dyn std::error::Error>> {
        let mut rng = SmallRng::from_entropy();
        let mut channels = BTreeSet::new();
        loop {
            let msg = read_msg(self.stream.clone()).await?;
            eprintln!("Received message {} {:?}", Utc::now(), msg);
            match msg.state {
                DiscoveryState::Connect => self.on_connect(msg, &mut rng).await?,
                DiscoveryState::ConnectResponse => self.on_connect_response(msg).await?,
                DiscoveryState::QueueData => self.on_queue_data(msg, &mut channels).await?,
                DiscoveryState::Error => self.on_error(msg).await?,
            }
        }
    }
}

pub async fn run_server(addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
    let pool = ThreadPool::new().unwrap();
    let listener = TcpListener::bind(addr).await.unwrap();
    eprintln!("Server is listening on {:?}", listener.local_addr());
    let mut incoming = listener.incoming();
    while let Some(stream) = incoming.next().await {
        let stream = stream?.clone();
        pool.spawn(async move {
            let discovery_service = DiscoveryServer::new(stream.clone());
            match discovery_service.run_loop().await {
                Ok(_) => (),
                Err(e) => {
                    eprintln!(
                        "failed connection from {} error: {}",
                        stream.peer_addr().unwrap(),
                        e
                    );
                }
            }
        })?;
    }

    Ok(())
}

fn local_address() -> Url {
    use std::net::{IpAddr, Ipv4Addr};
    SocketAddr::new(IpAddr::V4(Ipv4Addr::new(127, 0, 0, 1)), 0);
    urlparse::urlparse("tcp://127.0.0.1:0")
}

fn process_msg(msg: &DiscoveryMessage, channels: &[Channel]) -> DiscoveryMessage {
    match msg.state {
        DiscoveryState::Error => DiscoveryMessage {
            state: DiscoveryState::Connect,
            uri: local_address(),
            channels: channels.to_vec(),
        },
        DiscoveryState::Connect => DiscoveryMessage {
            state: DiscoveryState::ConnectResponse,
            uri: local_address(),
            channels: channels.to_vec(),
        },
        DiscoveryState::ConnectResponse => DiscoveryMessage {
            state: DiscoveryState::QueueData,
            uri: local_address(),
            channels: channels.to_vec(),
        },
        DiscoveryState::QueueData => DiscoveryMessage {
            state: DiscoveryState::QueueData,
            uri: local_address(),
            channels: channels.to_vec(),
        },
    }
}

pub async fn run_client(
    server: SocketAddr,
    channels: &[Channel],
) -> Result<(), Box<dyn std::error::Error>> {
    loop {
        let stream = TcpStream::connect(server).await?;
        let msg = read_msg(stream).await?;
        let next_msg = process_msg(&msg, &channels);
        if next_msg.state == DiscoveryState::QueueData {
            // tcpserver.init(next_msg.uri,channels); send mmessages to tcpservice using channels
        } else {
            eprintln!("Waiting for 10 seconds");
            sleep(std::time::Duration::from_secs(10)).await;
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use futures::executor::block_on;
    use std::str::FromStr;
    #[test]
    fn ds_server() {
        let _ = block_on(async { run_server(SocketAddr::from_str("0.0.0.0:9999").unwrap()).await });
    }
}
