use crate::msg_serde::{MsgFormat, SmartMonitorMsg};
use crate::smart_monitor_sqlite::{select_all_msg, select_msg};
use async_std::sync::{Arc, Mutex};
use chrono::NaiveDateTime;
use log::*;
use rusqlite::{Connection, OpenFlags};
use smallvec::SmallVec;
use tide::{self, Request};

fn get_all_channel_data(
    req: &Request<()>,
    connections: &[Connection],
) -> Result<std::vec::Vec<smallvec::SmallVec<[SmartMonitorMsg; 1024]>>, Box<dyn std::error::Error>>
{
    let start = NaiveDateTime::from_timestamp(req.param::<i64>("start")?, 0);
    let end = NaiveDateTime::from_timestamp(req.param::<i64>("end")?, 0);
    connections
        .iter()
        .map(|conn| select_all_msg(conn, &start, &end))
        .collect::<Result<Vec<SmallVec<[SmartMonitorMsg; 1024]>>, _>>()
}

pub async fn web_service(channels: &[String]) -> Result<(), Box<dyn std::error::Error>> {
    let mut app = tide::new();
    let channels = channels.iter().cloned().collect::<Vec<String>>();
    app.at("/all").get(move |req: Request<()>| {
        let channels = channels.clone();
        let connections = channels
            .iter()
            .map(|ch| {
                Connection::open_with_flags(
                    &format!("{}.db3", ch),
                    OpenFlags::SQLITE_OPEN_READ_ONLY,
                )
                .unwrap()
            })
            .collect::<Vec<_>>();
        async move {
            match get_all_channel_data(&req, &connections) {
                Ok(v) => tide::Response::new(200).body_json(&v).unwrap(),
                Err(e) => {
                    error!("Error processing channels {}", e);
                    tide::Response::new(501).body_string(e.to_string())
                }
            }
        }
    });
    Ok(app.listen("0.0.0.0:8080").await?)
}
