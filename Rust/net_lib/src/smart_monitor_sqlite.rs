use crate::msg_serde::{MonitorMsg, MsgFormat, Payload};
use rusqlite::{params, Connection, DatabaseName};
use std::io::Write;

pub fn create_channel_table(name: &str) -> Result<Connection, Box<dyn std::error::Error>> {
    let conn = Connection::open(&format!("{}.db3", name))?;
    conn.execute(
        "CREATE TABLE IF NOT EXISTS TS_DATA (Timestamp TEXT PRIMARY KEY NOT NULL UNIQUE,
            Enter BOOLEAN NOT NULL, Format INTEGER NOT NULL, Msg BLOB NULL DEFAULT (x''))",
        params![],
    )?;
    Ok(conn)
}

pub fn insert(conn: &Connection, msg: &MonitorMsg) -> Result<usize, Box<dyn std::error::Error>> {
    match &msg.payload {
        Payload::Entry => {
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
            let mut blob = conn.blob_open(DatabaseName::Main, "TS_DATA", "Msg", rowid, false)?;
            blob.write(&msg.data)?;
            Ok(1)
        }
        Payload::Exit => Ok(conn.execute(
            "INSERT INTO TS_DATA (Timestamp,Enter,Format) VALUES (?1,?2,?3)",
            params![msg.adj_time_stamp, 1, 0],
        )?),
    }
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
}
