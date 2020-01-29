use crate::error::MeshError;
use async_std::net::{TcpListener, TcpStream};
use async_std::prelude::*;
use byteorder::{BigEndian, ByteOrder};
use futures::channel::mpsc::{channel, Receiver, Sender};
use futures::executor::{block_on, ThreadPool};
use futures::task::SpawnExt;
use serde::{Deserialize, Serialize};
use std::cell::RefCell;
use std::collections::BTreeMap;
use std::marker::PhantomData;
use std::mem::transmute;
use std::net::Ipv4Addr;

type ChannelId = u32;

pub trait Queue {
    fn decode(&self, pool: &mut ThreadPool, bytes: Vec<u8>) -> Result<(), MeshError>;
    fn as_mut_queue(&mut self) -> &mut dyn Queue;
    fn as_queue(&self) -> &dyn Queue;
    fn channel_id(&self) -> ChannelId;
}

pub trait Service {
    fn run<'service, 'a, 'b: 'a>(&self, tcp: &'service TcpService<'a, 'b>);
    fn as_service(&self) -> &dyn Service;
    fn queues(&self) -> Vec<&dyn Queue>;
}

pub struct FifoQueue<T> {
    sender: Sender<T>,
    receiver: Receiver<T>,
    channel_id: ChannelId,
}

impl<'service, T> FifoQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + 'service + Send,
{
    pub fn new(channel_id: ChannelId) -> Self {
        let (sender, receiver) = channel(100);
        Self {
            sender,
            receiver,
            channel_id,
        }
    }
}

impl<'a, 'service, T: 'static> Queue for RefCell<FifoQueue<T>>
where
    T: for<'de> Deserialize<'de> + Send,
{
    fn decode(&self, pool: &mut ThreadPool, bytes: Vec<u8>) -> Result<(), MeshError> {
        let mut sender = self.borrow().sender.clone();
        pool.spawn(async move {
            let result = bincode::deserialize::<T>(&bytes).unwrap();
            sender.try_send(result).unwrap();
        })
        .expect("Failed");
        Ok(())
    }

    fn as_queue(&self) -> &dyn Queue {
        self
    }

    fn as_mut_queue(&mut self) -> &mut dyn Queue {
        self
    }

    fn channel_id(&self) -> ChannelId {
        self.borrow().channel_id
    }
}

pub trait SingleItemQueue {
    type Target;
    fn pop(&mut self) -> Result<Self::Target, MeshError>;
    fn push(&mut self, item: Self::Target);
}

impl<T> SingleItemQueue for FifoQueue<T> {
    type Target = T;
    fn pop(&mut self) -> Result<Self::Target, MeshError> {
        self.receiver
            .try_next()?
            .ok_or_else(|| MeshError::ChannelError)
    }
    fn push(&mut self, item: Self::Target) {
        // If the channel exists then we serialise and send
        // else must be local and we just push to the next queue
        self.sender
            .try_send(item)
            .expect("Could not push onto queue");
    }
}

pub struct TcpService<'a, 'b> {
    senders: BTreeMap<ChannelId, &'a dyn Queue>,
    pool: ThreadPool,
    observers: BTreeMap<ChannelId, Vec<TcpStream>>,
    services: BTreeMap<ChannelId, Vec<&'b dyn Service>>, //, ChannelId>,
    phantom: PhantomData<&'a u8>,
}

impl<'a, 'b: 'a> TcpService<'a, 'b> {
    pub fn new() -> Self {
        Self {
            senders: BTreeMap::new(),
            pool: ThreadPool::new().unwrap(),
            observers: BTreeMap::new(),
            services: BTreeMap::new(),
            phantom: PhantomData,
        }
    }

    pub async fn listen(&mut self) -> Result<(), MeshError> {
        let listener = TcpListener::bind("127.0.0.1:8156").await?;
        let mut incoming = listener.incoming();
        while let Some(stream) = incoming.next().await {
            let stream = stream?;
            let reader = &mut &stream;
            let mut channel_id = [0u8; 4];
            let mut byte_len = [0u8; 4];
            reader.read_exact(&mut channel_id).await?;
            reader.read_exact(&mut byte_len).await?;
            let channel_id = BigEndian::read_u32(&channel_id) as u32;
            let mut buffer = Vec::with_capacity(BigEndian::read_u32(&byte_len) as usize);
            reader.read_exact(&mut buffer).await?;

            match channel_id {
                0 => {
                    let new_ips =
                        bincode::deserialize::<BTreeMap<ChannelId, (Vec<u8>, u16)>>(&buffer)?;
                    for (channel_id, (ip, port)) in new_ips {
                        self.observers
                            .entry(channel_id)
                            .and_modify(|connections| {
                                // This will lead to zombie connections so we need to
                                // add some kind of detection/clean up routine
                                connections.push(block_on(async {
                                    TcpStream::connect((
                                        Ipv4Addr::new(ip[0], ip[1], ip[2], ip[3]),
                                        port,
                                    ))
                                    .await
                                    .expect(&format!(
                                        "Could not connect on a new {}.{}.{}.{}:{}",
                                        ip[0], ip[1], ip[2], ip[3], port
                                    ))
                                }));
                            })
                            .or_insert_with(|| {
                                vec![block_on(async {
                                    TcpStream::connect((
                                        Ipv4Addr::new(ip[0], ip[1], ip[2], ip[3]),
                                        port,
                                    ))
                                    .await
                                    .expect(&format!(
                                        "Could not connect on a new {}.{}.{}.{}:{}",
                                        ip[0], ip[1], ip[2], ip[3], port
                                    ))
                                })]
                            });
                    }
                }
                _ => {
                    if let Some(s) = self.senders.get_mut(&channel_id) {
                        // Decode the message
                        s.decode(&mut self.pool, buffer)
                            .expect("Failed to decode message");
                        self.run_cycle(&channel_id);
                    }
                }
            }
        }
        Ok(())
    }

    /// Registering the service
    pub fn register_service(mut self, service: &'b dyn Service) -> Self {
        service.queues().into_iter().for_each(|q| {
            self.senders.insert(q.channel_id(), q);
            self.services
                .entry(q.channel_id())
                .and_modify(|services| {
                    services.push(service);
                })
                .or_insert({ vec![service] });
        });
        self
    }

    fn run_cycle(&mut self, channel_id: &ChannelId) {
        if let Some(services) = self.services.get(&channel_id) {
            let services = services.clone();
            services.iter().for_each(|service| service.run(&self));
        }
    }

    pub fn send<T>(&self, channel_id: ChannelId, item: T) -> Result<(), MeshError>
    where
        T: Serialize + Clone,
    {
        const LOCALHOST: Ipv4Addr = Ipv4Addr::new(127, 0, 0, 1);

        if let Some(observers) = self.observers.get(&channel_id) {
            observers
                .iter()
                .map(|mut outward_connection| {
                    if outward_connection.local_addr().unwrap().ip() != LOCALHOST {
                        block_on(async {
                            // scoped thread here
                            let bytes = bincode::serialize(&item).expect("Could not serialise");
                            outward_connection
                                .write(&bytes)
                                .await
                                .expect("Could not send to channel");
                        })
                    } else {
                        if let Some(queue) = self.senders.get(&channel_id) {
                            queue.decode(&mut self.pool, vec![]);
                        }
                    }
                    Ok(())
                })
                .collect::<Result<(), MeshError>>()?;
        }
        Ok(())
    }

    pub fn ready(self) -> Self {
        self
    }
}

/*
+880-1-67-555-3794
+880-2-91-02-789
*/
