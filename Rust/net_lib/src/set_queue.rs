use crate::queues::PushByteData;
use crate::smart_monitor::{smlogger, SMLogger};
use async_std::sync::{Arc, Mutex};
use futures::{channel::mpsc, Future};
use serde::{Deserialize, Serialize};
use std::borrow::BorrowMut;
use std::collections::BTreeSet;
use std::pin::Pin;
use std::task::{Context, Poll};

// Output Queue
pub struct SetOutputQueue<T: Clone + Serialize + Send + Ord + 'static> {
    set_consumers: Vec<SetInputQueue<T>>,
    service: &'static str,
}

pub struct SetInputQueue<T>
where
    T: Serialize + PartialOrd + Ord,
{
    last_value: Arc<Mutex<Option<T>>>,
    values: BTreeSet<T>,
    sm_log: SMLogger<T>,
}

pub struct TcpSetOutputQueue<T>
where
    T: Serialize + Send + 'static,
{
    channel_id: &'static str,
    sender: mpsc::Sender<(&'static str, Vec<u8>)>,
    sm_log: SMLogger<T>,
}

impl<T> TcpSetOutputQueue<T>
where
    T: Serialize + Send + 'static,
{
    pub fn new(
        channel_id: &'static str,
        service: &'static str,
        sender: &mpsc::Sender<(&'static str, Vec<u8>)>, // Move this to a singleton?
    ) -> Self {
        Self {
            channel_id,
            sender: sender.clone(),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    pub fn send(&mut self, item: T) {
        self.sm_log.entry(&item);
        let bytes = bincode::serialize(&item).unwrap();
        self.sender.try_send((self.channel_id, bytes)).unwrap();
        self.sm_log.exit();
    }
}

// Implmentation of Output queue
// Split this into type specific queues
impl<T> SetOutputQueue<T>
where
    T: Clone + Ord + Serialize + Send + for<'de> Deserialize<'de> + std::fmt::Debug + 'static,
{
    pub fn new(service: &'static str) -> Self {
        Self {
            set_consumers: Vec::new(),
            service: service,
        }
    }

    pub fn set_input_queue(&'static mut self, channel_id: &'static str) -> &SetInputQueue<T> {
        self.set_consumers
            .push(SetInputQueue::new(channel_id, &self.service));
        self.set_consumers.last().unwrap()
    }

    pub fn send(&mut self, item: T) {
        for output in self.set_consumers.iter_mut() {
            output.push(item.clone());
        }
    }
}

impl<T> SetInputQueue<T>
where
    T: Clone + Serialize + for<'de> Deserialize<'de> + PartialOrd + Ord,
{
    fn new(channel_id: &'static str, service: &'static str) -> Self {
        Self {
            last_value: Arc::new(Mutex::new(None)),
            values: BTreeSet::new(),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    pub fn push(&mut self, item: T) {
        if !self.values.contains(&item) {
            let mut data = self.last_value.try_lock().unwrap();
            self.sm_log.entry(&item);
            self.values.insert(item.clone());
            *data = Some(item);
        }
    }
}

impl<T> Future for SetInputQueue<T>
where
    T: Serialize + std::marker::Unpin + for<'de> Deserialize<'de> + Ord + Copy,
{
    type Output = T;
    fn poll(mut self: Pin<&mut Self>, ctx: &mut Context) -> Poll<Self::Output> {
        if let Some(value) = {
            self.last_value
                .try_lock()
                .map_or_else(|| None, |v| v.or(None))
        } {
            self.borrow_mut().sm_log.exit();
            Poll::Ready(value)
        } else {
            ctx.waker().wake_by_ref();
            Poll::Pending
        }
    }
}

impl<T> PushByteData for SetInputQueue<T>
where
    T: Clone + Serialize + for<'de> Deserialize<'de> + Ord + Send,
{
    fn push_data(&mut self, v: &[u8]) {
        let v = bincode::deserialize::<T>(v).expect("Could not deserialise from bytes");
        self.push(v);
    }
}
