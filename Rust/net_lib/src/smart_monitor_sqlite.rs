use crate::msg_serde::{MonitorMsg, MsgFormat, Payload, SmartMonitorMsg};
use chrono::NaiveDateTime;
use log::*;
use num_traits::FromPrimitive;
use rusqlite::{params, Connection, DatabaseName};
use smallvec::SmallVec;
use std::io::{Read, Write};

pub fn create_channel_table(name: &str) -> Result<Connection, Box<dyn std::error::Error>> {
    let conn = Connection::open(&format!("{}.db3", name))?;
    conn.execute(
        "CREATE TABLE IF NOT EXISTS TS_DATA (Timestamp TEXT PRIMARY KEY NOT NULL UNIQUE,
            Enter BOOLEAN NOT NULL, Format INTEGER NOT NULL, Msg BLOB NULL DEFAULT (x''))",
        params![],
    )?;
    // Latency
    conn.execute(
        "CREATE TABLE IF NOT EXISTS LATENCY (Timestamp TEXT PRIMARY KEY NOT NULL UNIQUE, VALUE DOUBLE NOT NULL)",
        params![],
    )?;
    // Memory
    conn.execute(
        "CREATE TABLE IF NOT EXISTS MEMORY (Timestamp TEXT PRIMARY KEY NOT NULL UNIQUE, VALUE DOUBLE NOT NULL)",
        params![],
    )?;
    // CPU
    conn.execute(
        "CREATE TABLE IF NOT EXISTS CPU (Timestamp TEXT PRIMARY KEY NOT NULL UNIQUE, VALUE DOUBLE NOT NULL)",
        params![],
    )?;
    Ok(conn)
}

pub fn insert(conn: &Connection, msg: &MonitorMsg) -> Result<usize, Box<dyn std::error::Error>> {
    match &msg.payload {
        Payload::Entry | Payload::Error => {
            debug!("Writing to database entry/error message");
            let msg_format = match msg.msg_format {
                MsgFormat::Bincode => 0,
                MsgFormat::MsgPack => 1,
                MsgFormat::Json => 2,
            };
            conn.execute(
                &format!(
                    "INSERT INTO TS_DATA (Timestamp,Enter,Format,Msg) VALUES (?1,?2,?3,ZEROBLOB({}))",
                    msg.data.len()
                ),
                params![msg.adj_time_stamp, 1, msg_format],
            )?;
            let rowid = conn.last_insert_rowid();
            debug!("Writing blob with row_id {}", rowid);
            let mut blob = conn.blob_open(DatabaseName::Main, "TS_DATA", "Msg", rowid, false)?;
            blob.write(&msg.data)?;
            Ok(rowid as usize)
        }
        Payload::Latency => Ok(conn.execute(
            "INSERT INTO LATENCY (Timestamp,VALUE) VALUES (?1,?2)",
            params![msg.adj_time_stamp, msg.data.first().unwrap()],
        )?),
        Payload::Cpu => Ok(conn.execute(
            "INSERT INTO CPU (Timestamp,VALUE) VALUES (?1,?2)",
            params![msg.adj_time_stamp, msg.data.first().unwrap()],
        )?),
        Payload::Memory => Ok(conn.execute(
            "INSERT INTO MEMORY (Timestamp,VALUE) VALUES (?1,?2)",
            params![msg.adj_time_stamp, msg.data.first().unwrap()],
        )?),
        Payload::Exit => Ok(conn.execute(
            "INSERT INTO TS_DATA (Timestamp,Enter,Format) VALUES (?1,?2,?3)",
            params![msg.adj_time_stamp, 1, 0],
        )?),
    }
}

// Select all messages in the queue
pub fn select_all_msg(
    conn: &Connection,
    start: &NaiveDateTime,
    end: &NaiveDateTime,
) -> Result<SmallVec<[SmartMonitorMsg; 64]>, Box<dyn std::error::Error>> {
    let mut stmt = conn.prepare(
        "SELECT ROWID,TIMESTAMP,ENTER,FORMAT FROM TS_DATA WHERE TIMESTAMP >= ?1 AND TIMESTAMP <= ?2",
    )?;
    let mut rows = stmt.query(params![start, end])?;
    let mut results = SmallVec::<[SmartMonitorMsg; 64]>::new();
    while let Some(row) = rows.next()? {
        let row_id = row.get::<usize, i64>(0)?;
        let ts = row.get::<usize, NaiveDateTime>(1)?;
        let payload = FromPrimitive::from_u8(row.get::<usize, u8>(2)?).unwrap();
        let encoding = FromPrimitive::from_i64(row.get::<usize, i64>(3)?).unwrap();
        trace!("Retrieving row {} {} ", row_id, ts);
        results.push(SmartMonitorMsg {
            row_id,
            ts,
            encoding,
            payload,
            data: SmallVec::<[u8; 1024]>::new(),
        })
    }
    Ok(results)
}

// Drill down to specific message
pub fn select_msg(
    conn: &Connection,
    row_id: i64,
) -> Result<SmartMonitorMsg, Box<dyn std::error::Error>> {
    trace!("Retrieving blob for row {} ", row_id);
    let mut blob = conn.blob_open(DatabaseName::Main, "TS_DATA", "Msg", row_id, false)?;
    let sz = blob.size() as usize;
    let mut data = SmallVec::<[u8; 1024]>::new();
    data.resize(sz, 0u8);
    blob.read_exact(&mut data)?;
    trace!("Succeeded reading {} bytes", data.len());
    Ok(SmartMonitorMsg {
        row_id,
        ts: NaiveDateTime::from_timestamp(0, 0),
        encoding: MsgFormat::Bincode,
        payload: Payload::Entry,
        data,
    })
}

#[cfg(test)]
mod tests {
    use super::*;
    use crate::msg_serde::*;
    use chrono::prelude::*;
    #[test]
    fn insert_db() {
        let utc = Utc::now();
        let msg = MonitorMsg {
            channel_name: ChannelId::from("hello"),
            adj_time_stamp: utc.naive_local(),
            msg_format: MsgFormat::Json,
            payload: Payload::Entry,
            data: b"123456".to_vec(),
        };
        let conn = create_channel_table("channel1").unwrap();
        insert(&conn, &msg).expect("could not insert");
    }

    #[test]
    fn select_db() {
        let conn = Connection::open_with_flags(
            "nasdaqtestin.db3",
            rusqlite::OpenFlags::SQLITE_OPEN_READ_ONLY,
        )
        .unwrap();
        select_all_msg(
            &conn,
            &NaiveDateTime::from_timestamp(0, 0),
            &NaiveDateTime::from_timestamp(1000, 0),
        )
        .unwrap();
    }
}
