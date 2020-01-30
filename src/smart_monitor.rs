use crate::messages::*;
use crate::smart_monitor_sqlite;
use crate::utils::*;
use async_std::io::ReadExt;
use async_std::net::SocketAddr;
use async_std::net::{TcpListener, TcpStream};
use async_std::stream::StreamExt;
use chrono::NaiveDateTime;
use futures::channel::mpsc::{channel, Receiver, Sender};
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use std::collections::BTreeMap;

pub struct SmartMonitor {
    pool: ThreadPool,
    senders: BTreeMap<ChannelId, Sender<Message>>,
}

#[derive(Clone)]
pub enum MessageType {
    Entry(Vec<u8>),
    Exit,
}

#[derive(Clone)]
pub struct Message {
    pub adj_time_stamp: NaiveDateTime,
    pub message_type: MessageType,
}

impl SmartMonitor {
    pub fn new() -> Self {
        Self {
            pool: ThreadPool::new().unwrap(),
            senders: BTreeMap::new(),
        }
    }
    async fn consume(
        name: &str,
        mut msg_receiver: Receiver<Message>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        let conn = smart_monitor_sqlite::create_channel_table(name)?;
        while let Ok(msg) = msg_receiver.try_next() {
            if let Some(msg) = msg {
                smart_monitor_sqlite::insert(&conn, &msg)?;
            }
        }
        Ok(())
    }
    pub fn create(&mut self, ch: ChannelId) -> Result<(), Box<dyn std::error::Error>> {
        let (sender, receiver) = channel(10);
        self.pool.spawn(async move {
            match SmartMonitor::consume(&radix_str(ch as u64), receiver).await {
                Ok(_) => (),
                Err(_) => eprintln!("failed saving to database"),
            }
        })?;
        self.senders.insert(ch, sender);
        Ok(())
    }

    async fn handle_monitoring(
        stream: &mut TcpStream,
        mut sender: Sender<Message>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        loop {
            let mut buf = [0u8; 8];
            stream.read_exact(&mut buf).await?;
            let dt = i64::from_le_bytes(buf);
            stream.read_exact(&mut buf).await?;
            let msg_sz = i64::from_le_bytes(buf);
            match msg_sz {
                -1 => Ok(Message {
                    adj_time_stamp: NaiveDateTime::from_timestamp(
                        dt / 1000000,
                        (dt % 1000000) as u32,
                    ),
                    message_type: MessageType::Exit,
                }),
                any if any > 0 => {
                    let mut msg_buffer = vec![0u8; msg_sz as usize];
                    stream.read_exact(&mut msg_buffer[..]).await?;
                    Ok(Message {
                        adj_time_stamp: NaiveDateTime::from_timestamp(
                            dt / 1000000,
                            (dt % 1000000) as u32,
                        ),
                        message_type: MessageType::Entry(msg_buffer),
                    })
                }
                _ => Err("Unexpected size of message received".into())
                    as Result<Message, Box<dyn std::error::Error>>,
            }
            .and_then(|msg: Message| Ok(sender.try_send(msg)?))?;
        }
    }

    pub async fn listen(&self, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
        let listener = TcpListener::bind(addr).await.unwrap();
        let mut incoming = listener.incoming();
        while let Some(stream) = incoming.next().await {
            let mut stream = stream.unwrap();
            // Latency measurement using https://en.wikipedia.org/wiki/Network_Time_Protocol
            // Measure clock difference
            // Send adjustment to client

            // Channel Id
            let mut buf = [0u8; 8];
            stream.read_exact(&mut buf).await.unwrap();
            let channel = usize::from_le_bytes(buf);
            eprintln!("Connected as ChannelID: {}", channel);
            if let Some(sender) = self.senders.get(&channel) {
                let sender = sender.clone();
                self.pool.spawn(async move {
                    match SmartMonitor::handle_monitoring(&mut stream, sender).await {
                        Ok(_) => (),
                        Err(e) => eprintln!("error {:?}", e),
                    }
                })?;
            }
        }
        Ok(())
    }
}
