use crate::error::*;
use crate::queue2::*;
use futures::executor::block_on;
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use serde_derive::{Deserialize, Serialize};

#[derive(Serialize, Deserialize)]
struct Order {
    price: f32,
}

#[derive(Serialize, Deserialize)]
struct Transaction {
    price: f32,
    volume: i32,
}

#[derive(Serialize, Deserialize)]
struct Request {
    ip: u64,
}

#[derive(Serialize, Deserialize)]
struct HeartBeat {
    timestamp: u64,
}

struct OrderService {
    q1: Queue<Order>,
    q2: Queue<i32>,
    q3: ConstQueue<u32>,
    q4: TimerQueue<i32>,
}

impl OrderService {
    fn new(tcp_service: &TcpService) -> Self {
        Self {
            q1: tcp_service.create_queue(3, 0),   // LastValue Queue
            q2: tcp_service.create_queue(2, 100), // Burst Queue
            q3: tcp_service.create_const_node(|| 0),
            q4: tcp_service.create_timer_push_queue(|| 0, 1000, 6),
        }
    }

    fn run(mut self, mut pool: ThreadPool) -> Result<(), MeshError> {
        loop {
            let v1 = pool.spawn(self.q1.pull());
            let v2 = pool.spawn(self.q2.pull())?;
            let v3 = pool.spawn(self.q3.pull())?;
            OrderService::do_something(v1, v2, &mut self.q4)?;
        }
        Ok(())
    }

    fn do_something(v1: Order, v2: i32, timer: &mut TimerQueue<i32>) -> Result<(), MeshError> {
        println!("v1: {}", v2);
        block_on(async {
            timer.push(23, 1000).await.expect("Failed timer waiting");
            timer.push(23, 1000).await.expect("Failed timer waiting");
            timer.push(23, 1000).await.expect("Failed timer waiting");
        });
        Ok(())
    }
}

struct TransactionService {
    q1: Queue<i32>,
    q2: Queue<Order>,
    q3: Queue<i32>,
    error_queue: Queue<String>,
}

impl TransactionService {
    fn new(tcp_service: &TcpService) -> Self {
        Self {
            q1: tcp_service.create_queue(2, 0),
            q2: tcp_service.create_queue(3, 0),
            q3: tcp_service.create_queue(4, 0),
            error_queue: tcp_service.create_queue(5, 10),
        }
    }

    fn run(mut self, mut pool: ThreadPool) -> Result<(), MeshError> {
        loop {
            let v1 = pool.spawn(self.q1.pull())?;
            let v2 = pool.spawn(self.q2.pull())?;
            match TransactionService::do_something(v1, v2) {
                Ok(v) => pool.spawn(self.q3.push(v))?,
                Err(_) => pool.spawn(self.error_queue.push(String::from("error")))?,
            }
        }
        Ok(())
    }

    fn do_something(v1: i32, v2: Order) -> Result<i32, MeshError> {
        println!("v1: {}", v1);
        Ok(123)
    }
}

pub fn register_services() -> Result<(), MeshError> {
    // Register all the input queues
    let mut tcp_service = TcpService::new_listener(8156u16, 0);
    // Services
    let order_service = OrderService::new(&tcp_service);
    let txn_service = TransactionService::new(&tcp_service);

    let pool = tcp_service.pool.clone();
    pool.clone().spawn(async move {
        txn_service.run(pool.clone());
    })?;
    let pool = tcp_service.pool.clone();
    pool.clone().spawn(async move {
        order_service.run(pool.clone());
    })?;

    block_on(async { tcp_service.listen().await })?;
    Ok(())
}
