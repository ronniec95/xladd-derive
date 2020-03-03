use crate::msg_serde::{ChannelType, MsgFormat};
use crate::smart_monitor_sqlite::{select_all_msg, select_msg};
use async_std::sync::{Arc, Mutex};
use rusqlite::{Connection, OpenFlags};
use serde_derive::Serialize;
use tide::{self, Request};

#[derive(Debug, Clone, Serialize)]
pub struct ChannelResponse {
    pub name: String,
    pub instance: usize,
    pub channel_type: ChannelType,
    pub encoding_type: MsgFormat, // This is either msgpack,json or binary
    pub addresses: Vec<String>,
}

pub async fn web_service(channels: &[String]) -> Result<(), Box<dyn std::error::Error>> {
    let connections = Arc::new(Mutex::new(
        channels
            .iter()
            .map(|ch| {
                Connection::open_with_flags(
                    &format!("{}.db3", ch),
                    OpenFlags::SQLITE_OPEN_READ_ONLY,
                )
                .unwrap()
            })
            .collect::<Vec<_>>(),
    ));
    let mut app = tide::with_state(connections);
    app.at("/all")
        .get(move |req: Request<Arc<Mutex<Vec<Connection>>>>| {
            let connections = req.state();
            async move {
                // if let Some(connections) = connections.try_lock() {
                //     for conn in *connections {}
                // }
                tide::Response::new(200)
            }
        });
    Ok(app.listen("0.0.0.0:8080").await?)
}
