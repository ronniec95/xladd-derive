use futures::executor::block_on;
use futures::executor::ThreadPool;
use futures::task::SpawnExt;
use net_lib::smart_monitor::*;
use simplelog::*;
use std::borrow::Cow;
use std::net::*;
use std::str::FromStr;
fn main() -> Result<(), Box<dyn std::error::Error>> {
    TermLogger::init(
        LevelFilter::Debug,
        ConfigBuilder::new()
            .set_time_level(LevelFilter::Error)
            .set_time_format_str("%Y-%m-%d %H:%M:%S%.3f")
            .build(),
        TerminalMode::Mixed, //File::create("smart_monitor.log").unwrap(),
    )
    .unwrap();

    let mut smc = SmartMonitorClient::new();
    let mut sender =
        smc.create_sender::<i64>("nasdaqtestin".to_string(), Cow::Borrowed("pull_service"));
    let pool = ThreadPool::new().unwrap();
    pool.spawn(async move {
        smc.run_auto_reconnect(SocketAddr::from_str("127.0.0.1:9900").unwrap())
            .await;
    })?;
    block_on(async {
        sender.entry(&1234);
        async_std::task::sleep(std::time::Duration::from_secs(30)).await;
    });
    Ok(())
}
