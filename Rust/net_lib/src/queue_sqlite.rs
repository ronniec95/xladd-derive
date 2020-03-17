use chrono::Utc;
use log::*;
use rusqlite::{params, Connection, DatabaseName};
use smallvec::SmallVec;
use std::io::{Read, Write};

pub fn create_service_table(name: &str) -> Result<Connection, Box<dyn std::error::Error>> {
    let conn = Connection::open(name)?;
    conn.execute(
        "CREATE TABLE IF NOT EXISTS SERVICE_DATA (Timestamp TEXT NOT NULL UNIQUE,CHANNEL_ID TEXT NOT NULL,
              LIVE INTEGER NOT NULL, Msg BLOB NULL DEFAULT (x''), PRIMARY KEY (Timestamp, CHANNEL_ID))",
        params![],
    )?;
    Ok(conn)
}

pub fn insert(
    conn: &Connection,
    channel_id: &str,
    data: &[u8],
) -> Result<usize, Box<dyn std::error::Error>> {
    conn.execute(
        &format!(
            "INSERT INTO SERVICE_DATA (Timestamp, Channel_ID, Live, Msg) VALUES (?1,?2,1,ZEROBLOB({}))",
            data.len()
        ),
        params![Utc::now(), channel_id],
    )?;
    let rowid = conn.last_insert_rowid();
    debug!("Writing blob with row_id {}", rowid);
    let mut blob = conn.blob_open(DatabaseName::Main, "SERVICE_DATA", "Msg", rowid, false)?;
    blob.write(&data)?;
    Ok(rowid as usize)
}

// Select all messages in the queue
pub fn select_all_msg(
    conn: &Connection,
    channel_id: &str,
) -> Result<SmallVec<[Vec<u8>; 64]>, Box<dyn std::error::Error>> {
    // Load all live messages
    let mut stmt = conn.prepare(
        "SELECT ROWID FROM SERVICE_DATA WHERE CHANNEL_ID=?1 AND LIVE=1 ORDER BY TIMESTAMP ASC",
    )?;
    let mut rows = stmt.query(params![channel_id])?;
    let mut results = SmallVec::<[Vec<u8>; 64]>::new();
    while let Some(row) = rows.next()? {
        let row_id = row.get::<usize, i64>(0)?;
        let mut blob = conn.blob_open(DatabaseName::Main, "SERVICE_DATA", "Msg", row_id, true)?;
        let sz = blob.size() as usize;
        let mut data = vec![0u8; sz];
        blob.read_exact(&mut data)?;
        results.push(data);
    }
    // Mark as dead, all the messages that have been processes
    let sz = conn.execute(
        "DELETE FROM SERVICE_DATA WHERE CHANNEL_ID=?1 AND LIVE=1",
        params![channel_id],
    )?;
    assert_eq!(sz, results.len());
    Ok(results)
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn queue_insert_db() {
        let conn = create_service_table("service.db3").unwrap();
        insert(&conn, "hello", b"test data").unwrap();
        let res = select_all_msg(&conn, "hello").unwrap();
        assert!(res.len() == 1);
    }
}
