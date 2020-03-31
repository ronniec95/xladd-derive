use futures::channel::mpsc;
use log::*;
use net_lib::{
    queues,
    scalar_queue::{LastValueScalarInputQueue, TcpScalarOutputQueue},
};
use simplelog::*;

const SERVICE: &str = "main_service";
const CH1: &str = "ch1";
const CH2: &str = "ch2";
const MSQ: &str = "main_service_results";

struct MainService {
    pub q0: LastValueScalarInputQueue<i64>,
    pub q1: LastValueScalarInputQueue<i64>,
    pub q2: TcpScalarOutputQueue<i64>,
}

impl MainService {
    fn new(sender: &mpsc::Sender<(&'static str, Vec<u8>)>) -> Self {
        Self {
            q0: LastValueScalarInputQueue::new(CH1, SERVICE),
            q1: LastValueScalarInputQueue::new(CH2, SERVICE),
            q2: TcpScalarOutputQueue::new(MSQ, SERVICE, &sender), // Multiple outputs
        }
    }

    async fn run(&mut self) {
        let value = self.q0.clone().await;
        eprintln!("{}", value);
        self.q2.send(45);
    }
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    TermLogger::init(
        LevelFilter::Debug,
        ConfigBuilder::new()
            .set_time_level(LevelFilter::Error)
            .set_time_format_str("%Y-%m-%d %H:%M:%S%.3f")
            .build(),
        TerminalMode::Mixed,
    )
    .unwrap();

    let queue_mgr = queues::QueueManager::new();

    let sender = queue_mgr.sender();
    let mut listener = queue_mgr.listener();

    let mut main_service = MainService::new(&sender);
    listener.add_input(
        main_service.q0.channel_id,
        Box::new(main_service.q0.clone()),
    );
    listener.add_input(
        main_service.q1.channel_id,
        Box::new(main_service.q1.clone()),
    );

    let mut another_service = MainService::new(&sender);
    listener.add_input(
        main_service.q0.channel_id,
        Box::new(another_service.q0.clone()),
    );
    listener.add_input(
        main_service.q1.channel_id,
        Box::new(another_service.q1.clone()),
    );

    queue_mgr.run_service(async move { another_service.run().await });
    queue_mgr.run_service(async move { main_service.run().await });
    queue_mgr.start(listener);
    Ok(())
}
