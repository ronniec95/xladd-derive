use crate::msg_serde::*;
use crate::smart_monitor_sqlite;
use crate::smart_monitor_ws;
use async_std::net::SocketAddr;
use async_std::net::{TcpListener, TcpStream};
use async_std::stream::StreamExt;
use async_std::sync::{Arc, Mutex};
use chrono::Utc;
use futures::channel::mpsc::{unbounded, UnboundedReceiver, UnboundedSender};
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use log::*;
use serde::Serialize;
use std::borrow::Cow;
use std::collections::BTreeMap;
use std::sync::Once;

fn handle_error<U, E>(f: Result<U, E>)
where
    E: std::fmt::Display,
{
    match f {
        Ok(_) => (),
        Err(e) => error!("Error when processing {}", e),
    }
}
//
// Server Implmentation
//
pub struct SmartMonitor<'a> {
    pool: ThreadPool,
    senders: BTreeMap<Cow<'a, str>, Arc<Mutex<UnboundedSender<Cow<'static, MonitorMsg>>>>>,
}

impl<'a> SmartMonitor<'static> {
    pub fn new() -> Self {
        Self {
            pool: ThreadPool::new().unwrap(),
            senders: BTreeMap::new(),
        }
    }

    async fn consume(
        name: Cow<'a, str>,
        mut msg_receiver: UnboundedReceiver<Cow<'static, MonitorMsg>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        let conn = smart_monitor_sqlite::create_channel_table(&name)?;
        debug!("Connection to database ok {}", name);
        loop {
            if let Ok(msg) = msg_receiver.try_next() {
                trace!("Got message on receiver, attempting to write to db");
                if let Some(msg) = msg {
                    handle_error::<usize, _>(smart_monitor_sqlite::insert(&conn, &msg));
                }
            } else {
                async_std::task::sleep(std::time::Duration::from_micros(100)).await;
            }
        }
    }

    pub fn create(&mut self, ch: Cow<'static, str>) -> Result<(), Box<dyn std::error::Error>> {
        let (sender, receiver) = unbounded();
        self.pool.spawn({
            let channel_name = ch.to_owned();
            async move {
                match SmartMonitor::consume(channel_name, receiver).await {
                    Ok(_) => (),
                    Err(e) => error!("failed creating database {:?}", e),
                }
            }
        })?;
        self.senders.insert(ch, Arc::new(Mutex::new(sender)));

        Ok(())
    }

    async fn manage_monitor(
        mut stream: TcpStream,
        sender_map: BTreeMap<Cow<'_, str>, Arc<Mutex<UnboundedSender<Cow<'_, MonitorMsg>>>>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        let mut latency = 0i64;
        loop {
            let mut msg: Cow<'_, MonitorMsg> = Cow::Owned(read_sm_message(&mut stream).await?);
            debug!("Got smart mon msg {:?}", msg);
            match msg.payload {
                Payload::Latency => {
                    read_timestamp_async(&mut stream).await?;
                    let t1 = Utc::now().naive_utc();
                    write_timestamp_async(&mut stream, t1).await?;
                    let ntp = read_ntp_msg(&mut stream).await?;
                    latency = ntp.offset as i64 + ntp.delay as i64;
                    if let Some(sender) = sender_map.get(&Cow::Owned(msg.channel_name.clone())) {
                        msg.to_mut().adj_time_stamp =
                            msg.adj_time_stamp + chrono::Duration::milliseconds(latency);
                        trace!("Found channel {}", msg.channel_name);
                        let sender = sender.lock().await;
                        handle_error::<(), _>(sender.unbounded_send(msg.clone()));
                    }
                }
                Payload::Cpu | Payload::Memory | _ => {
                    if let Some(sender) = sender_map.get(&Cow::Owned(msg.channel_name.clone())) {
                        trace!("Found channel {}", msg.channel_name);
                        msg.to_mut().adj_time_stamp =
                            msg.adj_time_stamp + chrono::Duration::milliseconds(latency);
                        let sender = sender.lock().await;
                        handle_error::<(), _>(sender.unbounded_send(msg.clone()));
                    }
                }
            }
        }
    }

    // 07832271961

    pub async fn listen(&self, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
        {
            let channels = self.senders.keys().cloned().collect::<Vec<_>>();
            self.pool.spawn(async move {
                match smart_monitor_ws::web_service(&channels).await {
                    Ok(_) => (),
                    Err(e) => error!("error serving website {:?}", e),
                }
            })?;
        }
        let listener = TcpListener::bind(addr).await.unwrap();
        info!("Listening on {:?}", listener.local_addr());
        let mut incoming = listener.incoming();
        while let Some(stream) = incoming.next().await {
            let stream = stream?;
            let sender_map = self.senders.clone();
            self.pool.spawn(async move {
                match SmartMonitor::manage_monitor(stream, sender_map).await {
                    Ok(_) => (),
                    Err(e) => error!("error {:?}", e),
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
    sender: UnboundedSender<MonitorMsg>,
    receiver: UnboundedReceiver<MonitorMsg>,
}

pub fn smlogger() -> &'static mut SmartMonitorClient {
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
    sender: UnboundedSender<MonitorMsg>,
    channel: &'static str,
    service: Cow<'static, str>,
    _phantom: std::marker::PhantomData<T>,
}

impl SmartMonitorClient {
    pub fn new() -> Self {
        let (sender, receiver) = unbounded();
        Self { sender, receiver }
    }

    pub fn create_sender<T>(&self, channel: &'static str, service: &'static str) -> SMLogger<T>
    where
        T: Serialize,
    {
        SMLogger {
            sender: self.sender.clone(),
            channel,
            service: Cow::Borrowed(service),
            _phantom: std::marker::PhantomData,
        }
    }

    pub async fn run_auto_reconnect(&mut self, addr: SocketAddr) {
        loop {
            match self.run(addr).await {
                Ok(_) => (),
                Err(e) => error!("Socket disconnected, retrying after a few seconds {:?}", e),
            }
            async_std::task::sleep(std::time::Duration::from_secs(5)).await;
        }
    }
    async fn run(&mut self, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
        let mut conn = TcpStream::connect(addr).await?;
        info!("Connected to addr {}", addr);
        loop {
            while let Ok(msg) = self.receiver.try_next() {
                if let Some(msg) = msg {
                    write_sm_message(&mut conn, msg).await?;
                }
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
            channel_name: self.channel.to_string(),
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Entry,
            service: self.service.clone(),
            data: bytes,
        };
        if let Ok(_) = self.sender.unbounded_send(msg) {}
    }

    pub fn entry_vec(&mut self, item: &[T]) {
        let now = Utc::now();
        let bytes = bincode::serialize(&item).unwrap();
        let msg = MonitorMsg {
            channel_name: self.channel.to_string(),
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Entry,
            service: self.service.clone(),
            data: bytes,
        };
        if let Ok(_) = self.sender.unbounded_send(msg) {}
    }

    pub fn exit(&mut self) {
        let now = Utc::now();
        let msg = MonitorMsg {
            channel_name: self.channel.to_string(),
            adj_time_stamp: now.naive_utc(),
            msg_format: MsgFormat::Bincode,
            payload: Payload::Exit,
            service: self.service.clone(),
            data: vec![],
        };
        if let Ok(_) = self.sender.unbounded_send(msg) {}
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use futures::executor::block_on;

    #[test]
    fn no_server() {
        let smc = smlogger();
        //let sm = smc.create_sender::<i32>(123).clone();
        block_on(async {
            smc.run_auto_reconnect(SocketAddr::from(([127, 0, 0, 1], 3574)))
                .await;
        });
    }
}
