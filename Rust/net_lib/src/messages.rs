use chrono::NaiveDateTime;
use futures::io::{AsyncReadExt, AsyncWriteExt};
use num_derive::{FromPrimitive, ToPrimitive};
use std::borrow::Cow;
use std::cmp::{Ord, Ordering, PartialEq, PartialOrd};
use std::net::SocketAddr;

const DIGITS: &[u8] = "\rabcdefghijklmnopqrstuvwxyz0123456789._:/".as_bytes();
const DIGITLEN: usize = DIGITS.len();

#[derive(Clone, Debug)]
pub struct ChannelId {
    name: Vec<usize>,
}



#[derive(Clone, Copy, Ord, PartialEq, PartialOrd, Eq, Debug, FromPrimitive, ToPrimitive)]
pub enum MsgFormat {
    Bincode,
    MsgPack,
    Json,
}


#[derive(Copy, Clone, PartialEq, Debug, FromPrimitive, ToPrimitive)]
pub enum DiscoveryState {
    Connect = 1,
    ConnectResponse = 2,
    QueueData = 3,
    Error = 255,
}

#[derive(PartialEq, Debug, Copy, Clone, PartialOrd, Ord, Eq, FromPrimitive, ToPrimitive)]
pub enum ChannelType {
    Input,
    Output,
}

fn from_u64_slice(input: &[usize]) -> String {
    let s: Vec<u8> = input
        .iter()
        .map(|value| {
            let mut s = Vec::with_capacity(12);
            let mut value = *value as usize;
            while value > 0 {
                let ch = DIGITS[value % DIGITLEN];
                value = value / DIGITLEN;
                s.push(ch);
            }
            s.reverse();
            s
        })
        .flatten()
        .collect();
    String::from(std::str::from_utf8(&s).unwrap())
}

fn from_str_slice(input: &str) -> Vec<usize> {
    input
        .as_bytes()
        .chunks(12)
        .map(|data| {
            data.iter().fold(0, |v, ch| {
                v * DIGITLEN + DIGITS.iter().position(|&x| x == *ch).unwrap()
            })
        })
        .collect()
}

impl ChannelId {
    pub fn new(name: Vec<usize>) -> Self {
        Self { name }
    }
    pub fn byte_len(&self) -> usize {
        self.name.len() * 8 + 8
    }

    pub fn len(&self) -> u8 {
        self.name.len() as u8
    }

    pub fn as_bytes(&self) -> Vec<u8> {
        let mut bytes = Vec::with_capacity(self.byte_len());
        bytes.extend(&u8::to_le_bytes(self.name.len() as u8));
        self.name.iter().for_each(|v| {
            bytes.extend(&usize::to_le_bytes(*v));
        });
        bytes
    }
}

impl Ord for ChannelId {
    fn cmp(&self, other: &Self) -> Ordering {
        let lhs = self.name.first().unwrap();
        let rhs = other.name.first().unwrap();
        lhs.cmp(rhs)
    }
}

impl PartialOrd for ChannelId {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(self.cmp(other))
    }
}

impl PartialEq for ChannelId {
    fn eq(&self, other: &Self) -> bool {
        let lhs = self.name.first();
        let rhs = other.name.first();
        if let Some(lhs) = lhs {
            if let Some(rhs) = rhs {
                lhs == rhs
            } else {
                false
            }
        } else {
            false
        }
    }
}

impl Eq for ChannelId {}
impl From<&ChannelId> for String {
    fn from(id: &ChannelId) -> String {
        from_u64_slice(&id.name)
    }
}

impl<'a> From<&'a str> for ChannelId {
    fn from(name: &'a str) -> ChannelId {
        ChannelId {
            name: from_str_slice(name),
        }
    }
}

// *************************************************************
pub async fn channelid_from_async_reader<R>(
    reader: &mut &mut R,
) -> Result<ChannelId, Box<dyn std::error::Error>>
where
    R: AsyncReadExt + Unpin,
{
    let chunks = read_u8(reader).await?;
    let mut v = Vec::with_capacity(chunks as usize);
    for _ in 0..chunks as usize {
        v.push(read_u64(reader).await? as usize);
    }
    Ok(ChannelId { name: v })
}

pub fn channelid_from_reader<R>(reader: &mut R) -> Result<ChannelId, Box<dyn std::error::Error>>
where
    R: std::io::Read,
{
    let chunks = crate::net_reader::read_u8(reader)?;
    Ok(ChannelId {
        name: (0..chunks)
            .map(|_| crate::net_reader::read_u64(reader).unwrap() as usize)
            .collect(),
    })
}

pub async fn channelid_to_async_writer<W>(
    writer: &mut &mut W,
    channelid: &ChannelId,
) -> Result<(), Box<dyn std::error::Error>>
where
    W: AsyncWriteExt + Unpin,
{
    write_u8(writer, channelid.name.len() as u8).await?;
    for v in &channelid.name {
        write_u64(writer, *v as u64).await?;
    }
    Ok(())
}

pub fn channelid_to_writer<W>(
    writer: &mut W,
    channelid: &ChannelId,
) -> Result<(), Box<dyn std::error::Error>>
where
    W: std::io::Write,
{
    crate::net_reader::write_u8(writer, channelid.name.len() as u8)?;
    for v in &channelid.name {
        crate::net_reader::write_u64(writer, *v as u64)?;
    }
    Ok(())
}
// **********************************************************

#[derive(Clone, Debug)]
pub struct Channel<'a> {
    pub name: ChannelId,
    pub instance: usize,
    pub channel_type: ChannelType,
    pub encoding_type: MsgFormat, // This is either msgpack,json or binary
    pub addresses: Vec<Cow<'a, SocketAddr>>,
}

impl<'a> Ord for Channel<'a> {
    fn cmp(&self, other: &Self) -> Ordering {
        self.name.cmp(&other.name)
    }
}

impl<'a> PartialOrd for Channel<'a> {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(self.cmp(other))
    }
}

impl<'a> PartialEq for Channel<'a> {
    fn eq(&self, other: &Self) -> bool {
        self.name == other.name
    }
}

impl<'a> Eq for Channel<'a> {}

impl<'a> Channel<'a> {
    pub fn new(
        name: ChannelId,
        instance: usize,
        channel_type: ChannelType,
        encoding_type: MsgFormat,
    ) -> Self {
        Self {
            name,
            instance,
            channel_type,
            encoding_type,
            addresses: vec![],
        }
    }
}
// **********************************************************

#[derive(PartialEq, Debug)]
pub struct DiscoveryMessage<'a> {
    pub state: DiscoveryState,
    pub server: SocketAddr,
    pub channels: Vec<Channel<'a>>,
}

impl<'a> DiscoveryMessage<'a> {
    pub fn new(state: DiscoveryState, server: SocketAddr, channels: Vec<Channel<'a>>) -> Self {
        Self {
            state,
            server,
            channels,
        }
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    use num_traits::FromPrimitive;

    #[test]
    fn radix() {
        let s = "abmac.local:0";
        let num = ChannelId::from(s);
        dbg!(&DIGITS);
        dbg!(&num);
        let s2 = String::from(&num);
        assert_eq!(s2, "abmac.local:0");
    }

    #[test]
    fn primitive() {
        let x: DiscoveryState = FromPrimitive::from_u8(1).unwrap();
    }
}
