use crate::messages::*;
use crate::smart_monitor_sqlite;
use crate::utils::*;
use async_std::io::ReadExt;
use async_std::net::SocketAddr;
use async_std::net::{TcpListener, TcpStream};
use async_std::stream::StreamExt;
use chrono::{DateTime, NaiveDateTime, Utc};
use futures::channel::mpsc::{channel, Receiver, Sender};
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use serde::{Deserialize, Serialize};
use std::collections::BTreeMap;

//
// Server Implmentation
//
pub struct SmartMonitor {
    pool: ThreadPool,
    senders: BTreeMap<ChannelId, Sender<Message>>,
}

#[derive(Clone)]
pub enum Payload {
    Entry(Vec<u8>),
    Exit,
}

#[derive(Clone)]
pub enum MsgFormat {
    Bincode,
    MsgPack,
    Json,
}

#[derive(Clone)]
pub struct Message {
    pub adj_time_stamp: NaiveDateTime,
    pub msg_format: MsgFormat,
    pub payload: Payload,
}

async fn try_from_message<R>(reader: &mut R) -> Result<Message, Box<dyn std::error::Error>>
where
    R: ReadExt + Unpin,
{
    let mut buf = [0u8; 8];

    reader.read_exact(&mut buf).await?;
    let date = i64::from_le_bytes(buf);

    let mut buf_small = [0u8; 4];
    reader.read_exact(&mut buf_small).await?;
    let time = u32::from_le_bytes(buf_small);

    let mut byte = [0u8; 1];
    reader.read(&mut byte).await?;
    let msg_format = match byte[0] {
        0 => MsgFormat::Bincode,
        1 => MsgFormat::MsgPack,
        _ => MsgFormat::Json,
    };

    reader.read_exact(&mut buf).await?;
    let msg_payload = i64::from_le_bytes(buf);
    let payload: Result<Payload, Box<dyn std::error::Error>> = match msg_payload {
        -1 => Ok(Payload::Exit),
        n if n > 0 => {
            let mut buf = vec![0u8; n as usize];
            reader.read_exact(&mut buf).await?;
            Ok(Payload::Entry(buf))
        }
        _ => Err("Unexpected 0 size payload received".into()),
    };
    Ok(Message {
        adj_time_stamp: NaiveDateTime::from_timestamp(date, time),
        msg_format,
        payload: payload?,
    })
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
            let msg = try_from_message(stream)
                .await
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

//
// Client implmentation
//
pub struct SmartMonitorClient {
    sender: Sender<Message>,
    receiver: Receiver<Message>,
}

#[derive(Clone)]
pub struct SMLogger<T>
where
    T: Serialize,
{
    sender: Sender<Message>,
    _phantom: std::marker::PhantomData<T>,
}

impl SmartMonitorClient {
    pub fn new() -> Self {
        let (sender, receiver) = channel(10);
        Self { sender, receiver }
    }

    pub fn create_sender<T>(&self) -> SMLogger<T>
    where
        T: Serialize,
    {
        SMLogger {
            sender: self.sender.clone(),
            _phantom: std::marker::PhantomData,
        }
    }

    async fn run(mut self, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
        let conn = TcpStream::connect(addr).await?;
        // single connection to server, multiple senders, one per channel
        // Write binary
        loop {
            if let Some(msg) = self.receiver.try_next().unwrap() {
                let date = msg.adj_time_stamp.timestamp_millis();
                let time = msg.adj_time_stamp.timestamp_subsec_nanos();
            }
        }
    }
}

impl<T> SMLogger<T>
where
    T: Serialize,
{
    pub fn entry(&mut self, item: &T) {
        let now = Utc::now();
        let bytes = bincode::serialize(&item).unwrap();
        let msg = Message {
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Entry(bytes),
        };
        if let Ok(_) = self.sender.try_send(msg) {}
    }

    pub fn entry_vec(&mut self, item: &[T]) {
        let now = Utc::now();
        let bytes = bincode::serialize(&item).unwrap();
        let msg = Message {
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Entry(bytes),
        };
        if let Ok(_) = self.sender.try_send(msg) {}
    }

    pub fn exit(&mut self) {
        let now = Utc::now();
        let msg = Message {
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Exit,
        };
        if let Ok(_) = self.sender.try_send(msg) {}
    }
}
