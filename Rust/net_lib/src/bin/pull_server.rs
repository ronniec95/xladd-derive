use net_lib::queues::{LastValueQueue, OutputQueue, TcpQueueManager};
use std::borrow::Cow;
struct MainService {
    pub q0: LastValueQueue<i64>,
    pub q1: OutputQueue<i64>,
}

impl MainService {
    fn new(tcp_queue_mgr: &TcpQueueManager) -> Self {
        let init = Self {
            q0: LastValueQueue::new("inputchannel".to_string(), Cow::Borrowed("mainservice")), // Tcp input queue
            q1: OutputQueue::new("output_channel"), // Multiple outputs
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
