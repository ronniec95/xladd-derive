use crate::smart_monitor::{Message, MessageType};
use rusqlite::{params, Connection, DatabaseName, OpenFlags};
use std::io::Write;

pub fn create_channel_table(name: &str) -> Result<Connection, Box<dyn std::error::Error>> {
    let conn = Connection::open_with_flags(
        &format!("{:?}.db3", name),
        OpenFlags::SQLITE_OPEN_READ_WRITE | OpenFlags::SQLITE_OPEN_CREATE,
    )?;
    conn.execute(
        "CREATE TABLE IF NOT EXISTS TS_DATA (Timestamp INTEGER PRIMARY KEY NOT NULL UNIQUE,
            Enter BOOLEAN NOT NULL, Msg BLOB NULL DEFAULT (x''))",
        params![],
    )?;
    Ok(conn)
}

pub fn insert(conn: &Connection, msg: &Message) -> Result<usize, Box<dyn std::error::Error>> {
    match &msg.message_type {
        MessageType::Entry(data) => {
            conn.execute(
                "INSERT INTO TS_DATA (Timestamp,Enter) VALUES (?1,?2)",
                params![msg.adj_time_stamp, 1,],
            )?;
            let rowid = conn.last_insert_rowid();
            let mut blob = conn.blob_open(DatabaseName::Main, "TS_DATA", "Msg", rowid, false)?;
            blob.write(&data)?;
            Ok(1)
        }
        MessageType::Exit => Ok(conn.execute(
            "INSERT INTO TS_DATA (Timestamp,Enter) VALUES (?1,?2)",
            params![msg.adj_time_stamp, 1],
        )?),
    }
}
