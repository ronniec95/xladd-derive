use crossbeam::scope;
use log::*;
use net_lib::{
    queues,
    scalar_queue::{ScalarInputQueue, ScalarOutputQueue, TcpOutputQueue},
};
use simplelog::*;

const SERVICE: &str = "main_service";
const CH1: &str = "ch1";
const CH2: &str = "ch2";
const MSQ: &str = "main_service_results";

struct Producer {
    out1: ScalarOutputQueue<i64>,
    out2: ScalarOutputQueue<i64>,
}

impl Producer {
    fn new() -> Self {
        Self {
            out1: ScalarOutputQueue::new(),
            out2: ScalarOutputQueue::new(),
        }
    }

    fn run(&self) {
        for i in 0..10 {
            self.out1.send(i);
            if i % 2 == 0 {
                self.out2.send(i * 5);
            }
        }
    }
}

struct Consumer {
    in1: ScalarInputQueue<i64>,
    in2: ScalarInputQueue<i64>,
}

impl Consumer {
    fn new(in1: ScalarInputQueue<i64>, in2: ScalarInputQueue<i64>) -> Self {
        Self { in1, in2 }
    }
    fn run(&mut self) {
        loop {
            Consumer::run_impl(&mut self.in1, &mut self.in2);
        }
    }

    fn run_impl(in1: &mut ScalarInputQueue<i64>, in2: &mut ScalarInputQueue<i64>) {
        println!("In1 {}", in1.next());
        println!("In2 {}", in2.next());
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

    let producer = Producer::new();
    let mut consumer = Consumer::new(producer.out1.new_consumer(), producer.out2.new_consumer());
    scope(|scope| {
        scope.spawn(|_| {
            producer.run();
        });
        consumer.run();
    })
    .unwrap();

    /*
    listener.add_producer(
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
    */
    Ok(())
}
