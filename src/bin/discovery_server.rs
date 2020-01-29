use net_lib::discovery_service;
use std::net::{IpAddr,Ipv4Addr,SocketAddr};
use futures::executor::block_on;
use pico_args;

struct Args {
    port: u16,
}

fn main() -> Result<(), Box<dyn std::error::Error>> {
    let mut args = pico_args::Arguments::from_env();
    let args = Args {
        port: args.value_from_str(["-p","--port"])?,
    };
    let addr = SocketAddr::new(IpAddr::V4(Ipv4Addr::new(0,0,0,0)),args.port);
    let server_addr = addr.clone();
    block_on(async {
        match discovery_service::DiscoveryServer::listen(server_addr).await {
            Ok(_) => (),
            Err(e) => eprintln!("{:?}", e),
        }
    });
    Ok(())
}
