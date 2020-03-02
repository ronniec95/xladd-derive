use futures::executor::block_on;
use log::*;
use net_lib::discovery_service;
use pico_args;
use simplelog::*;
use std::fs::File;
use std::net::{IpAddr, Ipv4Addr, SocketAddr};

struct Args {
    port: u16,
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    WriteLogger::init(
        LevelFilter::Debug,
        ConfigBuilder::new()
            .set_time_level(LevelFilter::Error)
            .set_time_format_str("%Y-%m-%d %H:%M:%S%.3f")
            .build(),
        File::create("discovery.log").unwrap(),
    )
    .unwrap();
    let mut args = pico_args::Arguments::from_env();
    let args = Args {
        port: args.value_from_str(["-p", "--port"])?,
    };
    let addr = SocketAddr::new(IpAddr::V4(Ipv4Addr::new(0, 0, 0, 0)), args.port);
    let server_addr = addr.clone();
    block_on(async {
        match discovery_service::run_server(server_addr).await {
            Ok(_) => (),
            Err(e) => error!("{:?}", e),
        }
    });
    Ok(())
}
