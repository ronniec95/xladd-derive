use crate::messages::*;
use crate::smart_monitor_sqlite;
use crate::utils::*;
use async_std::io::prelude::*;
use async_std::net::SocketAddr;
use async_std::net::{TcpListener, TcpStream};
use async_std::stream::StreamExt;
use chrono::{NaiveDateTime, Utc};
use futures::channel::mpsc::{channel, Receiver, Sender};
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use serde::Serialize;
use std::collections::BTreeMap;
use std::sync::{Arc, Mutex, Once};

//
// Server Implmentation
//
pub struct SmartMonitor {
    pool: ThreadPool,
    senders: Arc<Mutex<BTreeMap<ChannelId, Sender<Message>>>>,
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
    pub channel_name: ChannelId,
    pub adj_time_stamp: NaiveDateTime,
    pub msg_format: MsgFormat,
    pub payload: Payload,
}

fn try_from_message<R>(reader: &mut R) -> Result<Message, Box<dyn std::error::Error>>
where
    R: std::io::Read,
{
    let mut buf = [0u8; 8];

    reader.read_exact(&mut buf)?;
    let channel_name = usize::from_le_bytes(buf);

    reader.read_exact(&mut buf)?;
    let date = i64::from_le_bytes(buf);

    let mut buf_small = [0u8; 4];
    reader.read_exact(&mut buf_small)?;
    let time = u32::from_le_bytes(buf_small);

    let mut byte = [0u8; 1];
    reader.read(&mut byte)?;
    let msg_format = match byte[0] {
        0 => MsgFormat::Bincode,
        1 => MsgFormat::MsgPack,
        _ => MsgFormat::Json,
    };

    reader.read(&mut byte)?;
    let msg_type = u8::from_le_bytes(byte);

    reader.read_exact(&mut buf)?;
    let msg_payload = usize::from_le_bytes(buf);
    let payload: Result<Payload, Box<dyn std::error::Error>> = match msg_type {
        0 => Ok(Payload::Exit),
        1 => {
            let mut buf = vec![0u8; msg_payload];
            reader.read_exact(&mut buf)?;
            Ok(Payload::Entry(buf))
        }
        _ => Err("Unexpected message type, not an entry or exit queue message".into()),
    };
    Ok(Message {
        channel_name,
        adj_time_stamp: NaiveDateTime::from_timestamp(date, time),
        msg_format,
        payload: payload?,
    })
}

impl From<Message> for Vec<u8> {
    fn from(msg: Message) -> Vec<u8> {
        let date = msg.adj_time_stamp.timestamp_millis();
        let time = msg.adj_time_stamp.timestamp_subsec_nanos();
        let msg_format = match msg.msg_format {
            MsgFormat::Bincode => 0u8,
            MsgFormat::MsgPack => 1u8,
            MsgFormat::Json => 2u8,
        };
        let (msg_type, sz, payload) = match msg.payload {
            Payload::Entry(payload) => (1u8, payload.len(), payload),
            Payload::Exit => (2u8, 0, vec![]),
        };
        let mut bytes = Vec::with_capacity(payload.len() + 30);
        bytes.extend(&usize::to_le_bytes(payload.len() + 30));
        bytes.extend(&usize::to_le_bytes(msg.channel_name));
        bytes.extend(&i64::to_le_bytes(date));
        bytes.extend(&u32::to_le_bytes(time));
        bytes.extend(&u8::to_le_bytes(msg_format));
        bytes.extend(&u8::to_le_bytes(msg_type));
        bytes.extend(&usize::to_le_bytes(sz));
        bytes.extend(&payload);
        bytes
    }
}

impl SmartMonitor {
    pub fn new() -> Self {
        Self {
            pool: ThreadPool::new().unwrap(),
            senders: Arc::new(Mutex::new(BTreeMap::new())),
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
                Err(e) => eprintln!("failed creating database {:?}", e),
            }
        })?;
        if let Ok(mut senders) = self.senders.try_lock() {
            senders.insert(ch, sender);
        }
        Ok(())
    }

    async fn handle_monitoring(
        stream: &mut TcpStream,
        sender_map: Arc<Mutex<BTreeMap<ChannelId, Sender<Message>>>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        loop {
            let mut buf = [0u8; 8];
            stream.read_exact(&mut buf).await?;
            let msg_size = usize::from_le_bytes(buf);
            let mut buf = vec![0u8; msg_size];
            stream.read_exact(&mut buf).await?;
            if let Ok(msg) = try_from_message(&mut std::io::Cursor::new(buf)) {
                loop {
                    if let Ok(mut sender_map) = sender_map.try_lock() {
                        if let Some(sender) = sender_map.get_mut(&msg.channel_name) {
                            sender.try_send(msg)?;
                        }
                        break;
                    }
                }
            }
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
            let sender_map = self.senders.clone();
            self.pool.spawn(async move {
                match SmartMonitor::handle_monitoring(&mut stream, sender_map).await {
                    Ok(_) => (),
                    Err(e) => eprintln!("error {:?}", e),
                }
            })?;
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

pub fn logger() -> &'static mut SmartMonitorClient {
    static mut SINGLETON: *mut SmartMonitorClient = 0 as *mut SmartMonitorClient;
    static ONCE: Once = Once::new();
    unsafe {
        ONCE.call_once(|| {
            let singleton = SmartMonitorClient::new();
            SINGLETON = std::mem::transmute(Box::new(singleton));
        });
        &mut (*SINGLETON)
    }
}

#[derive(Clone)]
pub struct SMLogger<T>
where
    T: Serialize,
{
    sender: Sender<Message>,
    channel: ChannelId,
    _phantom: std::marker::PhantomData<T>,
}

impl SmartMonitorClient {
    pub fn new() -> Self {
        let (sender, receiver) = channel(10);
        Self { sender, receiver }
    }

    pub fn create_sender<T>(&self, channel: ChannelId) -> SMLogger<T>
    where
        T: Serialize,
    {
        SMLogger {
            sender: self.sender.clone(),
            channel,
            _phantom: std::marker::PhantomData,
        }
    }

    pub async fn run_auto_reconnect(&mut self, addr: SocketAddr) {
        loop {
            match self.run(addr).await {
                Ok(_) => (),
                Err(e) => eprintln!("Socket disconnected, retrying after a few seconds {:?}", e),
            }
            async_std::task::sleep(std::time::Duration::from_secs(5)).await;
        }
    }
    async fn run(&mut self, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
        let mut conn = TcpStream::connect(addr).await?;
        loop {
            if let Ok(msg) = self.receiver.try_next() {
                if let Some(msg) = msg {
                    let bytes: Vec<u8> = msg.into();
                    conn.write_all(&bytes).await?;
                }
            } else {
                eprintln!("No message to receive, waiting 2s");
                async_std::task::sleep(std::time::Duration::from_secs(2)).await;
            }
        }
    }
}

impl<T> SMLogger<T>
where
    T: Serialize,
{
    pub fn entry(&mut self, item: &T) {
        if cfg!(test) {
            return;
        }

        let now = Utc::now();
        let bytes = bincode::serialize(&item).unwrap();
        let msg = Message {
            channel_name: self.channel,
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
            channel_name: self.channel,
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Entry(bytes),
        };
        if let Ok(_) = self.sender.try_send(msg) {}
    }

    pub fn exit(&mut self) {
        let now = Utc::now();
        let msg = Message {
            channel_name: self.channel,
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Exit,
        };
        if let Ok(_) = self.sender.try_send(msg) {}
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use futures::executor::block_on;

    #[test]
    fn msg_parsing() {
        use std::io::Read;
        let msg = Message {
            channel_name: 45236784,
            adj_time_stamp: Utc::now().naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Entry(b"123456".to_vec()),
        };
        let mut bytes = Vec::<u8>::from(msg.clone());

        // Read header first
        let mut cursor = std::io::Cursor::new(&mut bytes);
        let mut buf = [0u8; 8];
        cursor.read_exact(&mut buf).unwrap();
        let sz = usize::from_le_bytes(buf);
        assert_eq!(sz, 36);
        let msg2 = try_from_message(&mut cursor).unwrap();
        assert_eq!(msg.channel_name, msg2.channel_name);
    }

    #[test]
    fn no_server() {
        let smc = logger();
        //let sm = smc.create_sender::<i32>(123).clone();
        block_on(async {
            smc.run_auto_reconnect(SocketAddr::from(([127, 0, 0, 1], 3574)))
                .await;
        });
    }
}
