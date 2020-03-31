use crate::msg_serde::SmartMonitorMsg;
use crate::smart_monitor_sqlite::{select_all_msg, select_msg};
use chrono::NaiveDateTime;
use log::*;
use rusqlite::{Connection, OpenFlags};
use smallvec::SmallVec;
use std::borrow::Cow;
use std::sync::{Arc, Mutex};
use tide::{self, Request};

fn get_all_channel_data(
    start: NaiveDateTime,
    end: NaiveDateTime,
    webconnection: Arc<Mutex<Vec<(String, Connection)>>>,
) -> Vec<smallvec::SmallVec<[SmartMonitorMsg; 64]>> {
    info!("Request for all channels {} {}", start, end);
    if let Ok(connection) = webconnection.lock() {
        connection
            .iter()
            .map(
                |conn| match select_all_msg(&conn.1, &conn.0, &start, &end) {
                    Ok(v) => v,
                    Err(e) => {
                        error!("Failed to retrieve data from data {}", e);
                        SmallVec::<[SmartMonitorMsg; 64]>::new()
                    }
                },
            )
            .collect::<Vec<SmallVec<[SmartMonitorMsg; 64]>>>()
    } else {
        vec![]
    }
}

fn get_all_channel_data_start_end(
    req: &Request<()>,
    webconnection: Arc<Mutex<Vec<(String, Connection)>>>,
) -> Vec<smallvec::SmallVec<[SmartMonitorMsg; 64]>> {
    let start = NaiveDateTime::from_timestamp(req.param::<i64>("start").unwrap_or_default(), 0);
    let end = NaiveDateTime::from_timestamp(req.param::<i64>("end").unwrap_or_default(), 0);
    get_all_channel_data(start, end, webconnection)
}

fn get_all_channel_data_from_start(
    req: &Request<()>,
    webconnection: Arc<Mutex<Vec<(String, Connection)>>>,
) -> Vec<smallvec::SmallVec<[SmartMonitorMsg; 64]>> {
    let start = NaiveDateTime::from_timestamp(req.param::<i64>("start").unwrap_or_default(), 0);
    let end = chrono::MAX_DATE.and_hms(23, 59, 59).naive_local();
    get_all_channel_data(start, end, webconnection)
}

fn get_channel_msg(req: &Request<()>) -> Result<SmartMonitorMsg, Box<dyn std::error::Error>> {
    let channel_name = req.param::<String>("channel")?;
    let row_id = req.param::<i64>("row_id")?;
    info!("Request for msg  {}", row_id);
    let conn = Connection::open_with_flags(
        &format!("{}.db3", channel_name),
        OpenFlags::SQLITE_OPEN_READ_ONLY,
    )?;
    select_msg(&conn, row_id)
}

pub async fn web_service(channels: &[Cow<'_, str>]) -> Result<(), Box<dyn std::error::Error>> {
    let mut app = tide::new();
    let connections = Arc::new(Mutex::new(
        channels
            .iter()
            .map(|ch| {
                debug!("Connecting to {}.db3", ch);
                (
                    ch.to_string(),
                    Connection::open_with_flags(
                        &format!("{}.db3", ch),
                        OpenFlags::SQLITE_OPEN_READ_ONLY,
                    )
                    .unwrap(),
                )
            })
            .collect::<Vec<_>>(),
    ));

    let c = connections.clone();
    app.at("/all/:start/:end").get(move |req: Request<()>| {
        let c = c.clone();
        async move {
            let data = get_all_channel_data_start_end(&req, c);
            tide::Response::new(200)
                .set_header("Access-Control-Allow-Origin", "*")
                .body_json(&data)
                .unwrap()
        }
    });

    app.at("/all/:start").get(move |req: Request<()>| {
        let c = connections.clone();
        async move {
            let data = get_all_channel_data_from_start(&req, c);
            tide::Response::new(200)
                .set_header("Access-Control-Allow-Origin", "*")
                .body_json(&data)
                .unwrap()
        }
    });
    app.at("/msg/:channel/:row_id")
        .get(move |req: Request<()>| async move {
            match get_channel_msg(&req) {
                Ok(v) => tide::Response::new(200)
                    .set_header("Access-Control-Allow-Origin", "*")
                    .body_json(&v)
                    .unwrap(),
                Err(e) => {
                    error!("Error processing channels {}", e);
                    tide::Response::new(501).body_string(e.to_string())
                }
            }
        });
    Ok(app.listen("0.0.0.0:8081").await?)
}
