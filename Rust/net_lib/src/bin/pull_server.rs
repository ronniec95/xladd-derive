use net_lib::queues::{LastValueQueue, OutputQueue, TcpQueueManager};

struct MainService {
    pub q0: LastValueQueue<i64>,
    pub q1: OutputQueue<i64>,
}

impl MainService {
    fn new(tcp_queue_mgr: &TcpQueueManager) -> Self {
        let init = Self {
            q0: LastValueQueue::new("inputchannel".to_string()), // Tcp input queue
            q1: OutputQueue::new(),                              // Multiple outputs
        };
        init
    }
    async fn run(&mut self) {
        let value = self.q0.clone().await;
        eprintln!("{}", value);
        self.q1.send(45);
    }
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    Ok(())
}
