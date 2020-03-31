use crate::msg_serde::*;
use async_std::{
    net::{SocketAddr, TcpListener},
    stream::StreamExt,
};
use crossbeam::channel::Sender;
use futures::channel::oneshot;
use log::*;
use multimap::MultiMap;
/// Implements a tcp external connection for channels
///
/*
pub struct TcpTransportListener {
    input_queue_map: BTreeMap<&'static str, Vec<Box<dyn PushByteData>>>,
}

impl TcpTransportListener {
    /// Creates a listener on a port
    /// # Arguments
    /// * 'port' - A port number
    ///
    pub fn new() -> Self {
        Self {
            input_queue_map: BTreeMap::new(),
        }
    }

    /// Register a queue that's going to listen on a tcp port
    /// # Arguments
    /// * 'id' - channel id
    /// * 'sink' - queue that implments PushByteData
    ///
    pub fn add_input(&mut self, id: &'static str, sink: Box<dyn PushByteData>) {
        self.input_queue_map.entry(id).or_insert(vec![]).push(sink);
    }



    async fn listen(&mut self, port: u16) -> Result<(), Box<dyn std::error::Error>> {
        info!("Binding to port {}", port);
        let listener = TcpListener::bind(SocketAddr::from(([0, 0, 0, 0], port))).await?;
        let mut incoming = listener.incoming();
        while let Some(stream) = incoming.next().await {
            let stream = stream?;
            let msg = read_queue_message(stream).await?;
            if let Some(sinks) = self.input_queue_map.get_mut(&*msg.channel_name) {
                for sink in sinks {
                    sink.push_data(&msg.data)
                }
            }
        }
        Ok(())
    }
}

*/

pub struct TcpConnManager {
    producers: MultiMap<&'static str, Sender<Vec<u8>>>,
}

impl TcpConnManager {
    /// Creates a listener on a port
    /// # Arguments
    /// * 'port' - A port number
    ///
    pub fn new() -> Self {
        Self {
            producers: MultiMap::new(),
        }
    }

    pub fn add_producer(&mut self, channel: &'static str, sender: Sender<Vec<u8>>) {
        self.producers.insert(channel, sender);
    }

    pub async fn listen(&self, port: u16) -> Result<(), Box<dyn std::error::Error>> {
        println!("Binding to port {}", port);
        let listener = TcpListener::bind(SocketAddr::from(([0, 0, 0, 0], port))).await?;
        let mut incoming = listener.incoming();
        while let Some(stream) = incoming.next().await {
            let stream = stream?;
            let msg = read_queue_message(stream).await?;
            if let Some(sinks) = self.producers.get_vec(&*msg.channel_name) {
                for producer in sinks {
                    producer.send(msg.data.clone()).unwrap();
                }
            }
        }
        Ok(())
    }
}

pub fn port_channel() -> (oneshot::Sender<u16>, oneshot::Receiver<u16>) {
    oneshot::channel()
}

pub async fn listen_port_updates(
    tcp_conn: TcpConnManager,
    mut port_receiver: oneshot::Receiver<u16>,
) -> Result<(), Box<dyn std::error::Error>> {
    loop {
        if let Ok(port) = port_receiver.try_recv() {
            if let Some(port) = port {
                tcp_conn.listen(port).await?;
            }
        } else {
            async_std::task::sleep(std::time::Duration::from_millis(100)).await;
        }
    }
}
