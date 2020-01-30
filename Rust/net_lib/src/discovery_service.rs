use crate::discovery_msg::*;
use async_std::net::SocketAddr;
use async_std::net::{TcpListener, TcpStream};
use async_std::prelude::*;
use async_std::sync::{Arc, Mutex};
use async_std::task::sleep;
use futures::channel::mpsc::{channel, Receiver, Sender};
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use std::borrow::Cow;
use std::collections::{BTreeMap};
use std::convert::TryFrom;

enum Discover {
    UpdatePort(u16),
    UpdateChannel(SocketAddr),
}

pub struct DiscoveryClient<'a> {
    addr: SocketAddr,
    senders: Vec<Sender<Cow<'a, Channel>>>,
    receivers: Vec<Receiver<Cow<'a, Channel>>>,
    all_channels: Vec<Cow<'a, Channel>>,
}

async fn send_msg<'a>(
    stream: &'_ mut TcpStream,
    msg: DiscoveryMessage<'_>,
) -> Result<(), Box<dyn std::error::Error>> {
    dbg!(&msg);
    let bytes_msg = Vec::<u8>::try_from(&msg)?;
    let sz = bytes_msg.len();
    stream.write(&usize::to_le_bytes(sz)).await?;
    stream.write_all(&bytes_msg).await?;
    Ok(())
}


async fn recv_msg<'local>(
    stream: &mut TcpStream,
) -> Result<DiscoveryMessage<'local>, Box<dyn std::error::Error>> {
    let mut buf = [0u8; 8];
    stream.read_exact(&mut buf).await?;
    let sz = usize::from_le_bytes(buf);
    let mut msg = vec![0u8; sz];
    stream.read_exact(&mut msg).await?;
    // Read the discovery msg from msgpack
    DiscoveryMessage::try_from(msg.as_slice())
}

impl<'a> DiscoveryClient<'a> {
    pub fn new(addr: SocketAddr, all_channels: Vec<Cow<'a, Channel>>) -> Self {
        // we only care about output channels to receive updates on
        let (senders, receivers) = all_channels
            .iter()
            .filter_map(|ch| {
                if ch.channel_type == ChannelType::Output {
                    Some(channel(10))
                } else {
                    None
                }
            })
            .unzip();
        Self {
            addr,
            senders,
            receivers,
            all_channels,
        }
    }

    pub async fn manage_protocol(&self) {
        loop {
            match self.connect_to_server().await {
                Ok(_) => println!("OK"),
                Err(_) => eprintln!("Lost connection to discovery server...waiting 5 seconds to reconnect"),
            }
            sleep(std::time::Duration::from_secs(5)).await;
        }
    }

    pub async fn connect_to_server(&self) -> Result<(), Box<dyn std::error::Error>> {
        loop {
            let mut stream = TcpStream::connect(self.addr).await?;
            send_msg(
                &mut stream,
                DiscoveryMessage::new(
                    DiscoveryState::Connect,
                    self.addr,
                    self.all_channels.to_vec(),
                ),
            )
            .await?;
            loop {
                let msg = recv_msg(&mut stream).await?;
                dbg!(&msg);
                match msg.state {
                    DiscoveryState::Connect => (),
                    DiscoveryState::ConnectResponse => {
                        eprintln!("Connect Response received, requesting queue data");
                    }
                    DiscoveryState::QueueData => {
                        dbg!(&msg);
                    }
                    DiscoveryState::Error => {
                        eprintln!("Unexpected message from discovery server");
                    }
                }
                eprintln!("Waiting for 10 seconds");
                sleep(std::time::Duration::from_secs(10)).await;
                send_msg(
                    &mut stream,
                    DiscoveryMessage::new(
                        DiscoveryState::QueueData,
                        self.addr,
                        self.all_channels.to_vec(),
                    ),
                )
                .await?;
            }
        }
    }
}

pub struct DiscoveryServer {}

impl DiscoveryServer {
    async fn handle_msgs(
        mut stream: TcpStream,
        channel_list: Arc<Mutex<BTreeMap<Cow<'_, Channel>, SocketAddr>>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        loop {
            if let Some(mut channel_list) = channel_list.try_lock() {
                let msg = recv_msg(&mut stream).await?;
                dbg!(&msg);
                    let msgs = match msg.state {
                    DiscoveryState::Connect => {
                        let addr = stream.peer_addr()?;
                        for ch in msg.channels {
                            channel_list.insert(ch.clone(), addr);
                        }
                        channel_list
                            .iter()
                            .map(|(channel, addr)| {
                                DiscoveryMessage::new(
                                    DiscoveryState::QueueData,
                                    *addr,
                                    vec![Cow::Owned(*channel.clone())],
                                )
                            })
                            .collect()
                    }
                    DiscoveryState::ConnectResponse => vec![],
                    DiscoveryState::QueueData => channel_list
                        .iter()
                        .map(|(channel, addr)| {
                            DiscoveryMessage::new(
                                DiscoveryState::QueueData,
                                *addr,
                                vec![Cow::Owned(*channel.clone())],
                            )
                        })
                        .collect(),
                    DiscoveryState::Error => {
                        eprintln!("Unexpected error discover state received");
                        vec![]
                    }
                };
                for msg in msgs.into_iter() {
                    send_msg(&mut stream, msg).await?;
                }
            } else {
                sleep(std::time::Duration::from_secs(1)).await;
            }
        }
    }

    pub async fn listen(addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
        let pool = ThreadPool::new().unwrap();
        let listener = TcpListener::bind(addr).await.unwrap();
        eprintln!("Server is listening on {:?}", listener.local_addr());
        let mut incoming = listener.incoming();
        let channel_list: Arc<Mutex<BTreeMap<Cow<'_, Channel>, SocketAddr>>> =
            Arc::new(Mutex::new(BTreeMap::new()));

        while let Some(stream) = incoming.next().await {
            let stream = stream.unwrap();
            let peer_addr = stream.peer_addr().unwrap().clone();
            let ch_list = channel_list.clone();
            pool.spawn(async move {
                match DiscoveryServer::handle_msgs(stream, ch_list.clone()).await {
                    Ok(_) => (),
                    Err(e) => {
                        eprintln!("Failed connection {:?} {:?}",e, peer_addr);
                        let delete_channels = ch_list.clone();
                        loop {
                            if let Some(mut delete_channels) = delete_channels.try_lock() {
                                let to_delete =
                                    delete_channels.iter().filter_map(|(channel, addr)| {
                                        return if *addr == peer_addr {
                                            Some(channel.clone())
                                        } else {
                                            None
                                        };
                                    }).collect::<Vec<_>>();
                                for ch in to_delete {
                                    eprintln!("Deleting channels {:?}",ch);
                                    delete_channels.remove(&ch);
                                }
                            }
                        }
                    }
                };
            })
            .unwrap();
        }
        Ok(())
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use futures::executor::{block_on};
    #[test]
    fn protocol() {
        let pool = ThreadPool::new().expect("Could not create threadpool");

        let addr = SocketAddr::from(([0, 0, 0, 0], 3257));
        pool.spawn(async move {
            match DiscoveryServer::listen(addr).await {
                Ok(_) => (),
                Err(e) => eprintln!("{:?}", e),
            }
        }).expect("Could not spawn");

        let ms1_channels = vec![
            Cow::Owned(Channel::with_str_name("channel1", 13, ChannelType::Output)),
            Cow::Owned(Channel::with_str_name("channel2", 13, ChannelType::Output)),
        ];


        let addr = SocketAddr::from(([0, 0, 0, 0], 3257));
        let mut micro_service1_receiver = DiscoveryClient::new(addr, ms1_channels);
        std::thread::sleep(std::time::Duration::from_secs(2));
        block_on(async move {
            micro_service1_receiver
                .connect_to_server()
                .await
                .expect("Failed somewhere when receiving messages");
            while let Ok(channel) = micro_service1_receiver.receivers[0].try_next() {
                println!("{:?}", channel);
            }
        });
    }

    #[test]
    fn server() {
        use std::str::FromStr;
        let addr = SocketAddr::from_str("0.0.0.0:3257").expect("failed to parse");
        let server_addr = addr.clone();
        block_on(async {
            match DiscoveryServer::listen(server_addr).await {
                Ok(_) => (),
                Err(e) => eprintln!("{:?}", e),
            }
        });
    }

    #[test]
    fn client() {
        let ms1_channels = vec![
            Cow::Owned(Channel::with_str_name("channel1", 13, ChannelType::Output)),
            Cow::Owned(Channel::with_str_name("channel2", 13, ChannelType::Output)),
        ];
        let addr = SocketAddr::from(([127, 0, 0, 1], 3257));
        let mut micro_service1_receiver = DiscoveryClient::new(addr, ms1_channels);

        let ms2_channels = vec![
            Cow::Owned(Channel::with_str_name("channel1", 13, ChannelType::Input)),
            Cow::Owned(Channel::with_str_name("channel2", 13, ChannelType::Input)),
            Cow::Owned(Channel::with_str_name("channel3", 14, ChannelType::Output)),
        ];
        let mut micro_service2_receiver = DiscoveryClient::new(addr, ms2_channels);

        let pool = ThreadPool::new().expect("Could not create threadpool");

        pool.spawn(async move {
            match micro_service1_receiver.manage_protocol().await {
                _ => (),
            }
            while let Ok(channel) = micro_service1_receiver.receivers[0].try_next() {
                println!("{:?}", channel);
            }
        }).expect("Could not spawn awaiting thread pool");

        block_on(async move {
            micro_service2_receiver.manage_protocol().await;
            while let Ok(channel) = micro_service2_receiver.receivers[0].try_next() {
                println!("{:?}", channel);
            }
        });
    }
}
