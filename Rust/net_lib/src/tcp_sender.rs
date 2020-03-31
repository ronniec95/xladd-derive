use crate::msg_serde::*;
use crate::queue_sqlite;
use async_std::{
    net::TcpStream,
    sync::{Arc, Mutex},
};
use crossbeam::channel::{unbounded, Receiver, Sender};
use futures::{channel::mpsc, executor::ThreadPool, io::AsyncWriteExt, task::SpawnExt};
use log::*;
use multimap::MultiMap;
use rusqlite::Connection;
use std::collections::BTreeMap;

pub struct TcpSinkManager {
    producer: Sender<(&'static str, Vec<u8>)>,
    consumer: Receiver<(&'static str, Vec<u8>)>,
}

impl TcpSinkManager {
    pub fn new() -> Self {
        let (producer, consumer) = unbounded();
        Self { producer, consumer }
    }

    pub fn run(&self) {
        while let Ok((channel, payload)) = self.consumer.recv() {
            println!("Receiving messags {} {:?}", channel, payload);
        }
    }

    pub fn new_producer(&self) -> Sender<(&'static str, Vec<u8>)> {
        self.producer.clone()
    }
}

pub struct TcpTransportSender {
    multiplex_sender: mpsc::Sender<(&'static str, Vec<u8>)>,
    multiplex_receiver: mpsc::Receiver<(&'static str, Vec<u8>)>,
}

impl TcpTransportSender {
    pub fn new() -> Self {
        let (multiplex_sender, multiplex_receiver) = mpsc::channel(1);
        Self {
            multiplex_sender,
            multiplex_receiver,
        }
    }

    async fn run_sender(
        host: &str,
        mut receiver: mpsc::Receiver<(ChannelId, Vec<u8>)>,
        channels: &[String],
        conn: Arc<Mutex<Connection>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        let mut stream = TcpStream::connect(host).await?;
        // First attempt to write messages to the tcp connection from the database
        let conn = conn.lock().await;
        for ch in channels {
            let msgs = queue_sqlite::select_all_msg(&conn, ch)?;
            for msg in msgs {
                stream.write(ch.as_bytes()).await?;
                stream.write_all(&msg).await?;
            }
        }
        // Then wait for messages
        loop {
            if let Ok(channel_info) = receiver.try_next() {
                if let Some((channel_id, data)) = channel_info {
                    stream.write(channel_id.as_bytes()).await?;
                    stream.write_all(&data).await?;
                }
            } else {
                async_std::task::sleep(std::time::Duration::from_micros(100)).await;
            }
        }
    }

    async fn run_senders_2(
        host: &str,
        receiver: mpsc::Receiver<(ChannelId, Vec<u8>)>,
        channels: &[String],
        conn: Arc<Mutex<Connection>>,
    ) {
        let res = TcpTransportSender::run_sender(&host, receiver, &channels, conn).await;
        match res {
            Ok(_) => (),
            Err(e) => {
                error!(
                    "Disconnected from tcp connection {} with error  {}",
                    host, e
                );
            }
        }
    }
    pub async fn receive_channel_updates<'a>(
        mut self,
        pool: ThreadPool,
        tcp_senders: Arc<Mutex<MultiMap<String, mpsc::Sender<(String, Vec<u8>)>>>>,
        mut receiver: mpsc::Receiver<Vec<Channel>>,
    ) -> Result<(), Box<dyn std::error::Error>> {
        // Update the tcp connection list, by iterating over the list for matching ips/port
        let mut appname = std::env::current_exe()?;
        appname.set_extension("db3");
        let conn = Arc::new(Mutex::new(queue_sqlite::create_service_table(
            appname.to_str().unwrap_or("service.db3"),
        )?));
        pool.spawn({
            let tcp_senders = tcp_senders.clone();
            let conn = conn.clone();
            async move {
                loop {
                    if let Ok(msg) = self.multiplex_receiver.try_next() {
                        if let Some((channel_id, data)) = msg {
                            let mut tcp_senders = tcp_senders.lock().await;
                            if let Some(tcp_sender) = tcp_senders.get_mut(channel_id) {
                                tcp_sender.try_send((channel_id.to_string(), data)).unwrap();
                            } else {
                                // Write to database
                                let mut appname = std::env::current_exe()
                                    .unwrap_or(std::path::Path::new("service").to_path_buf());
                                appname.set_extension("db3");
                                let conn = conn.lock().await;
                                queue_sqlite::insert(&conn, &channel_id, &data).unwrap();
                            }
                        }
                    }
                }
            }
        })?;
        loop {
            if let Ok(channels) = receiver.try_next() {
                if let Some(channels) = channels {
                    debug!("channels received");
                    // Put all the addresses in a set to ensure we only have unique connections
                    let mut addresses = BTreeMap::new();
                    for ip in &channels {
                        for addr in &ip.addresses {
                            if let Some(host) = &addr.hostname {
                                if let Some(port) = addr.port {
                                    addresses
                                        .entry(format!("{}:{}", host, port))
                                        .or_insert(vec![ip.name.clone()]);
                                }
                            }
                        }
                    }
                    // Create a new map of channels to tcp sockets
                    for (host, channels) in addresses {
                        let tcp_senders = tcp_senders.clone();
                        pool.spawn({
                            let conn = conn.clone();
                            async move {
                                let (sender, receiver) = mpsc::channel(1);
                                {
                                    let mut tcp_senders = tcp_senders.lock().await;
                                    for ch in &channels {
                                        tcp_senders.insert(ch.clone(), sender.clone());
                                    }
                                }
                                TcpTransportSender::run_senders_2(&host, receiver, &channels, conn)
                                    .await;
                                {
                                    let mut tcp_senders = tcp_senders.lock().await;
                                    for ch in &channels {
                                        tcp_senders.remove(ch);
                                    }
                                }
                            }
                        })?;
                        // Now have a map of channel names to senders. Note that this is multiple senders to a multiple receivers
                    }
                }
            }
        }
    }

    // All messages are run through a single sender unfortunately
    pub fn new_sender(&self) -> mpsc::Sender<(&'static str, Vec<u8>)> {
        self.multiplex_sender.clone()
    }

    pub fn mplex_receiver(&mut self) -> &mut mpsc::Receiver<(&'static str, Vec<u8>)> {
        &mut self.multiplex_receiver
    }

    pub fn channels_channel() -> (mpsc::Sender<Vec<Channel>>, mpsc::Receiver<Vec<Channel>>) {
        mpsc::channel(1)
    }
}
