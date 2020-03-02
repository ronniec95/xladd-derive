use crate::msg_serde::Channel;
use crate::msg_serde::{ChannelType, MsgFormat};
use async_std::sync::{Arc, Mutex};
use serde_derive::Serialize;
use std::borrow::Cow;
use std::collections::BTreeMap;

#[derive(Debug, Clone, Serialize)]
pub struct ChannelResponse {
    pub name: String,
    pub instance: usize,
    pub channel_type: ChannelType,
    pub encoding_type: MsgFormat, // This is either msgpack,json or binary
    pub addresses: Vec<String>,
}

pub async fn web_service(channels: &[String]) -> Result<(), Box<dyn std::error::Error>> {
    let mut app = tide::new();
    app.at("/").get(|_| async move { tide::Response::new(200) });
    Ok(app.listen("0.0.0.0:8080").await?)
}
