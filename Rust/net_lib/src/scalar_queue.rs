use crate::queues::PushByteData;
use crate::smart_monitor::{smlogger, SMLogger};
use async_std::sync::{Arc, Mutex};
use futures::{channel::mpsc, Future};
use serde::{Deserialize, Serialize};
use std::borrow::BorrowMut;
use std::pin::Pin;
use std::task::{Context, Poll};

// Output Queue
pub struct ScalarOutputQueue<T: Clone + Serialize + Send + 'static> {
    last_value_consumers: Vec<LastValueScalarInputQueue<T>>,
    burst_consumers: Vec<BurstScalarInputQueue<T>>,
    service: &'static str,
}

// Input Queue
#[derive(Clone)]
pub struct BurstScalarInputQueue<T>
where
    T: Serialize,
{
    pub channel_id: &'static str,
    values: Arc<Mutex<Vec<T>>>,
    sm_log: SMLogger<T>,
}

#[derive(Clone)]
pub struct LastValueScalarInputQueue<T>
where
    T: Serialize,
{
    pub channel_id: &'static str,
    value: Arc<Mutex<Option<T>>>,
    sm_log: SMLogger<T>,
}

pub struct TcpScalarOutputQueue<T>
where
    T: Serialize + Send + 'static,
{
    pub channel_id: &'static str,
    sender: mpsc::Sender<(&'static str, Vec<u8>)>,
    sm_log: SMLogger<T>,
}

impl<T> TcpScalarOutputQueue<T>
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
impl<T> ScalarOutputQueue<T>
where
    T: Clone + Serialize + Send + for<'de> Deserialize<'de> + std::fmt::Debug + 'static,
{
    pub fn new(service: &'static str) -> Self {
        Self {
            last_value_consumers: Vec::new(),
            burst_consumers: Vec::new(),
            service: service,
        }
    }

    pub fn lv_input_queue(
        &'static mut self,
        channel_id: &'static str,
    ) -> &LastValueScalarInputQueue<T> {
        self.last_value_consumers
            .push(LastValueScalarInputQueue::new(channel_id, &self.service));
        self.last_value_consumers.last().unwrap()
    }

    pub fn burst_input_queue(&mut self, channel_id: &'static str) -> &BurstScalarInputQueue<T> {
        self.burst_consumers
            .push(BurstScalarInputQueue::new(channel_id, &*self.service));
        self.burst_consumers.last().unwrap()
    }

    pub fn send(&mut self, item: T) {
        for output in self.last_value_consumers.iter_mut() {
            output.push(item.clone());
        }
        for output in self.burst_consumers.iter_mut() {
            output.push(item.clone());
        }
    }
}

// Implementation of input queues
impl<T> BurstScalarInputQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de>,
{
    fn new(channel_id: &'static str, service: &'static str) -> Self {
        Self {
            channel_id,
            values: Arc::new(Mutex::new(Vec::new())),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    fn push(&mut self, item: T) {
        self.sm_log.entry(&item);
        let mut data = self.values.try_lock().unwrap();
        data.push(item);
    }
}

impl<T> Future for BurstScalarInputQueue<T>
where
    T: Serialize + Unpin + Clone,
{
    type Output = Vec<T>;
    fn poll(mut self: Pin<&mut Self>, ctx: &mut Context) -> Poll<Self::Output> {
        let res = if let Some(mut data) = self.values.try_lock() {
            if !data.is_empty() {
                Some(data.drain(..).collect())
            } else {
                None
            }
        } else {
            None
        };

        match res {
            Some(res) => {
                self.borrow_mut().sm_log.exit();
                Poll::Ready(res)
            }
            None => {
                ctx.waker().wake_by_ref();
                Poll::Pending
            }
        }
    }
}

// Implementation of input queues
impl<T> LastValueScalarInputQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug,
{
    pub fn new(channel_id: &'static str, service: &'static str) -> Self {
        Self {
            channel_id,
            value: Arc::new(Mutex::new(None)),
            sm_log: smlogger().create_sender::<T>(channel_id, service),
        }
    }

    fn push(&mut self, item: T) {
        self.sm_log.entry(&item);
        let mut data = self.value.try_lock().unwrap();
        *data = Some(item);
    }
}

impl<T> Future for LastValueScalarInputQueue<T>
where
    T: Serialize + std::marker::Unpin + for<'de> Deserialize<'de> + Copy,
{
    type Output = T;
    fn poll(mut self: Pin<&mut Self>, ctx: &mut Context) -> Poll<Self::Output> {
        if let Some(value) = { self.value.try_lock().map_or_else(|| None, |v| v.or(None)) } {
            self.borrow_mut().sm_log.exit();
            Poll::Ready(value)
        } else {
            ctx.waker().wake_by_ref();
            Poll::Pending
        }
    }
}

impl<T> PushByteData for BurstScalarInputQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug + Send + Sync,
{
    fn push_data(&mut self, v: &[u8]) {
        let v = bincode::deserialize::<T>(v).expect("Could not deserialise from bytes");
        self.push(v);
    }
}

impl<T> PushByteData for LastValueScalarInputQueue<T>
where
    T: Serialize + for<'de> Deserialize<'de> + std::fmt::Debug + Send + Sync,
{
    fn push_data(&mut self, v: &[u8]) {
        let v = bincode::deserialize::<T>(v).expect("Could not deserialise from bytes");
        self.push(v);
    }
}
