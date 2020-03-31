use crate::queues::PushByteData;
use crate::smart_monitor::{smlogger, SMLogger};
use async_std::sync::{Arc, Mutex};
use futures::{channel::mpsc, Future};
use serde::{Deserialize, Serialize};
use std::borrow::BorrowMut;
use std::collections::BTreeMap;
use std::pin::Pin;
use std::task::{Context, Poll};

// Output Queue
pub struct DictOutputQueue<T, U>
where
    T: Clone + for<'de> Deserialize<'de> + Serialize + Send + Ord + 'static,
    U: for<'de> Deserialize<'de> + Serialize + Send + 'static,
{
    dict_consumers: Vec<DictInputQueue<T, U>>,
    service: &'static str,
    _data: std::marker::PhantomData<(T, U)>,
}

pub struct TcpDictOutputQueue<T, U>
where
    T: Serialize + Send + 'static,
    U: Serialize + Send + 'static,
{
    channel_id: &'static str,
    sender: mpsc::Sender<(&'static str, Vec<u8>)>,
    sm_log: SMLogger<(T, U)>,
}

impl<T, U> TcpDictOutputQueue<T, U>
where
    T: Serialize + Send + 'static,
    U: Serialize + Send + 'static,
{
    pub fn new(
        channel_id: &'static str,
        service: &'static str,
        sender: &mpsc::Sender<(&'static str, Vec<u8>)>, // Move this to a singleton?
    ) -> Self {
        Self {
            channel_id,
            sender: sender.clone(),
            sm_log: smlogger().create_sender::<(T, U)>(channel_id, service),
        }
    }

    pub fn send(&mut self, item: T, value: U) {
        let bytes = bincode::serialize(&item).unwrap();
        self.sm_log.entry(&(item, value));
        self.sender.try_send((self.channel_id, bytes)).unwrap();
        self.sm_log.exit();
    }
}

// Implmentation of Output queue
// Split this into type specific queues
impl<T, U> DictOutputQueue<T, U>
where
    T: Clone + Ord + Serialize + Send + for<'de> Deserialize<'de> + 'static,
    U: Clone + Serialize + Send + for<'de> Deserialize<'de> + 'static,
{
    pub fn new(service: &'static str) -> Self {
        Self {
            dict_consumers: Vec::new(),
            service: service,
            _data: std::marker::PhantomData,
        }
    }

    pub fn dict_input_queue(&'static mut self, channel_id: &'static str) -> &DictInputQueue<T, U> {
        self.dict_consumers
            .push(DictInputQueue::<T, U>::new(channel_id, &self.service));
        self.dict_consumers.last().unwrap()
    }

    pub fn send(&mut self, item: T, value: U) {
        for output in self.dict_consumers.iter_mut() {
            output.push(item.clone(), value.clone());
        }
    }
}

pub struct DictInputQueue<T, U>
where
    T: Serialize + for<'de> Deserialize<'de> + PartialOrd + Ord,
    U: Serialize + for<'de> Deserialize<'de>,
{
    last_value: Arc<Mutex<Option<(T, U)>>>,
    values: BTreeMap<T, u64>,
    sm_log: SMLogger<(T, U)>,
}

impl<T, U> DictInputQueue<T, U>
where
    T: Clone + Serialize + for<'de> Deserialize<'de> + PartialOrd + Ord,
    U: Clone + Serialize + for<'de> Deserialize<'de>,
{
    fn new(channel_id: &'static str, service: &'static str) -> Self {
        Self {
            last_value: Arc::new(Mutex::new(None)),
            values: BTreeMap::new(),
            sm_log: smlogger().create_sender::<(T, U)>(channel_id, service),
        }
    }

    pub fn push(&mut self, item: T, value: U) {
        if !self.values.contains_key(&item) {
            self.values.insert(item.clone(), 0);
            let mut data = self.last_value.try_lock().unwrap();
            self.sm_log.entry(&(item.clone(), value.clone()));
            *data = Some((item, value));
        }
    }
}

impl<T, U> Future for DictInputQueue<T, U>
where
    T: Serialize + std::marker::Unpin + Copy + for<'de> Deserialize<'de> + Ord,
    U: Serialize + std::marker::Unpin + for<'de> Deserialize<'de> + Copy,
{
    type Output = (T, U);
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
        // if let Some(value) = self
        //     .last_value
        //     .try_lock()
        //     .map_or_else(|| None, |v| v.or(None))
        // {
        //     self.borrow_mut().sm_log.exit();
        //     Poll::Ready(value)
        // } else {
        //     ctx.waker().wake_by_ref();
        //     Poll::Pending
        // }
    }
}

impl<T, U> PushByteData for DictInputQueue<T, U>
where
    T: Clone + Serialize + for<'de> Deserialize<'de> + Ord + Send,
    U: Clone + Serialize + for<'de> Deserialize<'de> + Send,
{
    fn push_data(&mut self, v: &[u8]) {
        let v = bincode::deserialize::<(T, U)>(v).expect("Could not deserialise from bytes");
        self.push(v.0, v.1);
    }
}
