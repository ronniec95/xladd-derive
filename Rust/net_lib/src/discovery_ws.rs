use crate::msg_serde::Channel;
use crate::msg_serde::{ChannelType, MsgFormat};
use async_std::sync::{Arc, Mutex};
use serde_derive::Serialize;
use std::collections::BTreeSet;

#[derive(Debug, Clone, Serialize)]
pub struct ChannelResponse {
    pub name: String,
    pub instance: usize,
    pub channel_type: ChannelType,
    pub encoding_type: MsgFormat, // This is either msgpack,json or binary
    pub addresses: Vec<String>,
}

pub async fn web_service(
    channels: Arc<Mutex<BTreeSet<Channel>>>,
) -> Result<(), Box<dyn std::error::Error>> {
    let mut app = tide::new();
    app.at("/").get(move |_| {
        let channels = channels.clone();
        async move {
            let mut channel_res = Vec::new();

            if let Some(channel_set) = channels.try_lock() {
                for ch in channel_set.iter() {
                    channel_res.push(ChannelResponse {
                        name: ch.name.clone(),
                        instance: ch.instance,
                        channel_type: ch.channel_type,
                        encoding_type: ch.encoding_type,
                        addresses: ch.addresses.iter().map(|v| v.unparse()).collect(),
                    });
                }
            }
            tide::Response::new(200).body_json(&channel_res).unwrap()
        }
    });
    Ok(app.listen("0.0.0.0:8080").await?)
}
