use crate::queue::{to_packets, Packet, PacketSender};
use crate::service::ServiceName;
use futures::channel::mpsc::*;
use futures::executor::ThreadPool;
use futures::StreamExt;
use serde::{Deserialize, Serialize};
use serde_derive::{Deserialize, Serialize};
use std::sync::Arc;
use std::time::SystemTime;

const BUFSIZE: usize = 140;

#[derive(Serialize, Deserialize)]
struct HeartBeat {
    service: ServiceName,
    timestamp: SystemTime,
}

struct Message {
    packets: Vec<Packet>,
}

pub struct SMConnection<Q: PacketSender> {
    host: String,
    threadpool: ThreadPool,
    queues: Vec<Q>,
}

impl<Q> SMConnection<Q>
where
    Q: PacketSender,
{
    pub fn new(host: &str) -> SMConnection<Q> {
        let (sender, receiver) = channel::<Packet>(10);
        Self {
            host: String::from(host),
            threadpool: ThreadPool::new().unwrap(),
            queues: vec![],
        }
    }

    pub fn run(&mut self) -> Result<(), std::io::Error> {
        // Tcp connection to SM
        loop {
            //let packet = async { self.receiver.next().await };
        }
        Ok(())
    }

    fn push<T: 'static>(&mut self, channel_id: usize, msg: T)
    where
        T: Send + Sync + Serialize,
    {
        self.threadpool.spawn_ok(async move {
            let msg = Arc::from(msg);
            let channel_id = Arc::from(channel_id);
            move || to_packets(*channel_id, msg).iter().for_each(|_| {});
        });
    }
}
