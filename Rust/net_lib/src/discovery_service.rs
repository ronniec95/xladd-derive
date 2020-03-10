use crate::discovery_ws::web_service;
use crate::msg_serde::*;
use async_std::net::SocketAddr;
use async_std::net::{TcpListener, TcpStream};
use async_std::prelude::*;
use async_std::sync::{Arc, Mutex};
use async_std::task::sleep;
use futures::channel::mpsc::Sender;
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use log::*;
use rand::rngs::SmallRng;
use rand::{Rng, SeedableRng};
use std::borrow::Cow;
use std::collections::BTreeSet;
use urlparse::Url;

pub struct DiscoveryServer {
    stream: TcpStream,
}

impl DiscoveryServer {
    fn new(stream: TcpStream) -> Self {
        Self { stream }
    }

    async fn on_connect<'a>(
        &self,
        msg: DiscoveryMessage<'a>,
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
                        channels: Cow::Borrowed(&[]),
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
                        channels: Cow::Borrowed(&[]),
                    },
                )
                .await?;
            }
        }
        Ok(())
    }

    async fn on_connect_response<'a>(
        &self,
        _: DiscoveryMessage<'a>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        Ok(())
    }

    async fn on_queue_data<'a>(
        &self,
        msg: DiscoveryMessage<'a>,
        channels: &Arc<Mutex<BTreeSet<Channel>>>,
        local_channels: &mut BTreeSet<Channel>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        loop {
            for ch in msg.channels.iter() {
                local_channels.insert(ch.clone());
            }
            if let Some(mut channels) = channels.try_lock() {
                for ch in local_channels.iter() {
                    channels.insert(ch.clone());
                }

                write_msg(
                    self.stream.clone(),
                    DiscoveryMessage {
                        state: DiscoveryState::QueueData,
                        uri: msg.uri,
                        channels: Cow::Owned(
                            channels.iter().map(|ch| ch.clone()).collect::<Vec<_>>(),
                        ),
                    },
                )
                .await?;
                break;
            }
        }
        Ok(())
    }

    async fn on_error<'a>(
        &self,
        _: DiscoveryMessage<'a>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        Ok(())
    }

    async fn run_loop(
        &self,
        channels: &Arc<Mutex<BTreeSet<Channel>>>,
        mut local_channels: &mut BTreeSet<Channel>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        let mut rng = SmallRng::from_entropy();
        loop {
            let stream = self.stream.clone();
            let msg = read_msg(stream).await?;
            info!("Received message {}", msg);
            match msg.state {
                DiscoveryState::Connect => self.on_connect(msg, &mut rng).await?,
                DiscoveryState::ConnectResponse => self.on_connect_response(msg).await?,
                DiscoveryState::QueueData => {
                    self.on_queue_data(msg, channels, &mut local_channels)
                        .await?;
                }
                DiscoveryState::Error => self.on_error(msg).await?,
            }
        }
    }
}

pub async fn run_server(addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
    let pool = ThreadPool::new().unwrap();
    let channels = Arc::new(Mutex::new(BTreeSet::new()));

    let listener = TcpListener::bind(addr).await.unwrap();
    info!("Server is listening on {:?}", listener.local_addr());
    let mut incoming = listener.incoming();

    let web_channels = channels.clone();
    // Spawn off the web server
    pool.spawn(async {
        match web_service(web_channels).await {
            Ok(_) => (),
            Err(e) => error!("failed webservice error {}", e),
        }
    })?;
    while let Some(stream) = incoming.next().await {
        let stream = stream?.clone();
        let global_channels = channels.clone();
        pool.spawn(async move {
            let discovery_service = DiscoveryServer::new(stream.clone());
            let mut local_channels = BTreeSet::new();
            match discovery_service
                .run_loop(&global_channels, &mut local_channels)
                .await
            {
                Ok(_) => (),
                Err(e) => {
                    error!(
                        "failed connection from {} error: {}",
                        stream.peer_addr().unwrap(),
                        e
                    );
                    loop {
                        if let Some(mut channels) = global_channels.try_lock() {
                            for ch in local_channels.iter() {
                                debug!("Removing channels {}", ch);
                                channels.remove(ch);
                            }
                            break;
                        }
                    }
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

fn process_msg<'a>(msg: &'a DiscoveryMessage, channels: &'a [Channel]) -> DiscoveryMessage<'a> {
    match msg.state {
        DiscoveryState::Error => DiscoveryMessage {
            state: DiscoveryState::Connect,
            uri: local_address(),
            channels: Cow::Borrowed(channels),
        },
        DiscoveryState::Connect => DiscoveryMessage {
            state: DiscoveryState::ConnectResponse,
            uri: local_address(),
            channels: Cow::Borrowed(channels),
        },
        DiscoveryState::ConnectResponse => DiscoveryMessage {
            state: DiscoveryState::QueueData,
            uri: local_address(),
            channels: Cow::Borrowed(channels),
        },
        DiscoveryState::QueueData => DiscoveryMessage {
            state: DiscoveryState::QueueData,
            uri: local_address(),
            channels: Cow::Borrowed(channels),
        },
    }
}

pub async fn run_client<'a, 'b: 'a>(
    server: SocketAddr,
    channels: &'a [Channel],
    mut notifier: Sender<Vec<Channel>>,
) -> Result<(), Box<dyn std::error::Error>> {
    let stream = TcpStream::connect(server).await?;
    loop {
        let msg = read_msg(stream.clone()).await?;
        let next_msg = process_msg(&msg, &channels);
        if next_msg.state == DiscoveryState::QueueData {
            notifier.try_send(next_msg.channels.to_vec()).unwrap();
        } else {
            debug!("Waiting for 10 seconds");
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
