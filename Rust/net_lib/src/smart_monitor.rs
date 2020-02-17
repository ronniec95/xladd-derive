use crate::msg_serde::*;
use crate::smart_monitor_sqlite;
use async_std::net::SocketAddr;
use async_std::net::{TcpListener, TcpStream};
use async_std::stream::StreamExt;
use chrono::Utc;
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
    senders: Arc<Mutex<BTreeMap<ChannelId, Sender<MonitorMsg>>>>,
}

//01494790028 - Imran IBB solicitors
//

impl SmartMonitor {
    pub fn new() -> Self {
        Self {
            pool: ThreadPool::new().unwrap(),
            senders: Arc::new(Mutex::new(BTreeMap::new())),
        }
    }
    async fn consume(
        name: &str,
        mut msg_receiver: Receiver<MonitorMsg>,
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
        let channel_name = String::from(&ch);
        self.pool.spawn(async move {
            match SmartMonitor::consume(&channel_name, receiver).await {
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
        stream: TcpStream,
        sender_map: Arc<Mutex<BTreeMap<ChannelId, Sender<MonitorMsg>>>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        loop {
            if let Ok(msg) = read_sm_message(stream.clone()).await {
                eprintln!("Got smart mon msg {:?}", msg);
                loop {
                    if let Ok(mut sender_map) = sender_map.try_lock() {
                        if let Some(sender) = sender_map.get_mut(&msg.channel_name) {
                            sender.try_send(msg)?;
                        }
                        break;
                    }
                }
            } else {
                eprintln!(
                    "Error receiving msg on smart monitor {:?}",
                    stream.peer_addr()
                );
            }
        }
    }

    pub async fn listen(&self, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
        let listener = TcpListener::bind(addr).await.unwrap();
        eprintln!("Listening on {:?}", listener.local_addr());
        let mut incoming = listener.incoming();
        while let Some(stream) = incoming.next().await {
            let stream = stream?;
            // Latency measurement using https://en.wikipedia.org/wiki/Network_Time_Protocol
            // Measure clock difference
            // Send adjustment to client
            let sender_map = self.senders.clone();
            self.pool.spawn(async move {
                match SmartMonitor::handle_monitoring(stream, sender_map).await {
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
    sender: Sender<MonitorMsg>,
    receiver: Receiver<MonitorMsg>,
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
    sender: Sender<MonitorMsg>,
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
                    write_sm_message(&mut conn, msg).await?;
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
        let msg = MonitorMsg {
            channel_name: self.channel.clone(),
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Entry,
            data: bytes,
        };
        if let Ok(_) = self.sender.try_send(msg) {}
    }

    pub fn entry_vec(&mut self, item: &[T]) {
        let now = Utc::now();
        let bytes = bincode::serialize(&item).unwrap();
        let msg = MonitorMsg {
            channel_name: self.channel.clone(),
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Entry,
            data: bytes,
        };
        if let Ok(_) = self.sender.try_send(msg) {}
    }

    pub fn exit(&mut self) {
        let now = Utc::now();
        let msg = MonitorMsg {
            channel_name: self.channel.clone(),
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Exit,
            data: vec![],
        };
        if let Ok(_) = self.sender.try_send(msg) {}
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use futures::executor::block_on;

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
