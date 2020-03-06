use chrono::Utc;
use futures::executor::block_on;
use net_lib::smart_monitor::SmartMonitor;
use pico_args;
use serde_derive::Deserialize;
use simplelog::*;
use std::borrow::Cow;
use std::fs::File;
use std::io::Read;
use std::net::{IpAddr, Ipv4Addr, SocketAddr};

#[derive(Deserialize)]
struct Channels {
    port: u16,
    names: Vec<String>,
}

struct Args {
    filename: String,
}
fn main() -> Result<(), Box<dyn std::error::Error>> {
    WriteLogger::init(
        LevelFilter::Debug,
        ConfigBuilder::new()
            .set_time_level(LevelFilter::Error)
            .set_time_format_str("%Y-%m-%d %H:%M:%S%.3f")
            .build(),
        File::create(format!(
            "smart_monitor_{}.log",
            Utc::now().format("%Y%m%d_%H%M%S")
        ))
        .unwrap(), // Change path to have timestamp
    )
    .unwrap();

    let mut args = pico_args::Arguments::from_env();
    let args = Args {
        filename: args.value_from_str(["-c", "--config"])?,
    };
    let mut file = File::open(args.filename)?;
    let mut contents = String::new();
    file.read_to_string(&mut contents)?;
    let config: Channels = toml::from_str(&contents)?;
    let mut smart_monitor = SmartMonitor::new();
    for channel in &config.names {
        smart_monitor.create(Cow::Owned(channel.to_owned()))?;
    }
    block_on(async {
        smart_monitor
            .listen(SocketAddr::new(
                IpAddr::V4(Ipv4Addr::new(0, 0, 0, 0)),
                config.port,
            ))
            .await
            .unwrap();
    });

    // Separate smart monitor reader that aggregates for website
    //
    //
    // Replay mechanism. Give me the state of all queues as of time T
    // All queues request messages and populate
    // And then user sends "run until" message
    // Start/Stop/Pause/StartAt<time>
    // TODO
    // Smart monitor client/server test
    // HashSet/Dict queues
    // Webservers for discovery and smart monitor
    // Multiformat parsing bincode, msgpack, json
    Ok(())
}
