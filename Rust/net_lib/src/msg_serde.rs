use async_std::net::TcpStream;
use chrono::NaiveDateTime;
use cookie_factory::{bytes::*, combinator::slice, gen, GenError};
use futures::io::{AsyncReadExt, AsyncWriteExt};
use nom::bytes::streaming::*;
use nom::combinator::*;
use num_derive::{FromPrimitive, ToPrimitive};
use num_traits::{FromPrimitive, ToPrimitive};
use smallvec::SmallVec;
use std::cmp::Ordering;
use std::convert::TryInto;
use urlparse::{urlparse, urlunparse, Url};

pub type ChannelId = String;

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

#[derive(Clone, Copy, Ord, PartialEq, PartialOrd, Eq, Debug, FromPrimitive, ToPrimitive)]
pub enum MsgFormat {
    Bincode = 0,
    MsgPack = 1,
    Json = 2,
}
#[derive(Debug, Clone)]
pub struct Channel {
    pub name: String,
    pub instance: usize,
    pub channel_type: ChannelType,
    pub encoding_type: MsgFormat, // This is either msgpack,json or binary
    pub addresses: Vec<Url>,
}

impl Ord for Channel {
    fn cmp(&self, other: &Self) -> Ordering {
        self.name.cmp(&other.name)
    }
}

impl PartialOrd for Channel {
    fn partial_cmp(&self, other: &Self) -> Option<Ordering> {
        Some(self.cmp(other))
    }
}

impl PartialEq for Channel {
    fn eq(&self, other: &Self) -> bool {
        self.name == other.name
    }
}

impl Eq for Channel {}

#[derive(Debug, PartialEq)]
pub struct DiscoveryMessage {
    pub state: DiscoveryState,
    pub uri: Url,
    pub channels: Vec<Channel>,
}

#[derive(Clone, Ord, PartialEq, PartialOrd, Eq, FromPrimitive, ToPrimitive, Debug)]
pub enum Payload {
    Entry,
    Exit,
}

#[derive(Clone, PartialEq, Debug)]
pub struct MonitorMsg {
    pub channel_name: String,
    pub adj_time_stamp: NaiveDateTime,
    pub msg_format: MsgFormat,
    pub payload: Payload,
    pub data: Vec<u8>,
}

#[derive(Clone, PartialEq, Debug)]
pub struct QueueMessage {
    pub channel_name: String,
    pub data: Vec<u8>,
}

// Helper functions

async fn read_u32_async<R: AsyncReadExt + Unpin>(
    reader: &mut R,
) -> Result<u32, Box<dyn std::error::Error>> {
    let mut buf = [0u8; 4];
    reader.read_exact(&mut buf).await?;
    Ok(u32::from_le_bytes(buf))
}

async fn read_u64_async<R: AsyncReadExt + Unpin>(
    reader: &mut R,
) -> Result<u64, Box<dyn std::error::Error>> {
    let mut buf = [0u8; 8];
    reader.read_exact(&mut buf).await?;
    Ok(u64::from_le_bytes(buf))
}

fn read_enum<T: FromPrimitive>(input: &[u8]) -> nom::IResult<&[u8], T> {
    map(take(1usize), |v: &[u8]| {
        FromPrimitive::from_u8(v[0]).unwrap()
    })(input)
}

fn read_usize(input: &[u8]) -> nom::IResult<&[u8], usize> {
    map(take(std::mem::size_of::<usize>()), |v: &[u8]| {
        usize::from_le_bytes(v.try_into().unwrap())
    })(input)
}

fn read_u32(input: &[u8]) -> nom::IResult<&[u8], u32> {
    map(take(std::mem::size_of::<u32>()), |v: &[u8]| {
        u32::from_le_bytes(v.try_into().unwrap())
    })(input)
}

fn read_str(input: &[u8]) -> nom::IResult<&[u8], &str> {
    let (input, sz) = read_u32(input)?;
    map(take(sz as usize), |v| std::str::from_utf8(v).unwrap())(input)
}

fn write_enum<'a, T: ToPrimitive>(
    prim: &T,
    output: &'a mut [u8],
) -> Result<(&'a mut [u8], u64), GenError> {
    gen(le_u8(prim.to_u8().unwrap()), &mut output[..])
}

fn write_str<'a>(s: &str, output: &'a mut [u8]) -> Result<(&'a mut [u8], u64), GenError> {
    let (output, _) = gen(le_u32(s.len() as u32), output)?;
    gen(slice(s), output)
}

fn write_usize<'a>(s: usize, output: &'a mut [u8]) -> Result<(&'a mut [u8], u64), GenError> {
    gen(le_u64(s as u64), output)
}

fn write_u32<'a>(s: u32, output: &'a mut [u8]) -> Result<(&'a mut [u8], u64), GenError> {
    gen(le_u32(s), output)
}

//
// Channel
//

fn read_channel<'a>(input: &[u8]) -> nom::IResult<&[u8], Channel> {
    let (input, name) = read_str(input)?;
    let (input, instance) = read_usize(input)?;
    let (input, channel_type) = read_enum::<ChannelType>(input)?;
    let (input, encoding_type) = read_enum::<MsgFormat>(input)?;
    let (input, _payload) = read_str(input)?;
    let (input, address_cnt) = read_usize(input)?;
    let mut addresses = Vec::with_capacity(address_cnt);
    let mut input = input;

    for _ in 0..address_cnt {
        let (rest, address) = read_str(input)?;
        addresses.push(urlparse(address));
        input = rest;
    }
    Ok((
        input,
        Channel {
            name: name.to_string(),
            instance,
            channel_type,
            encoding_type,
            addresses,
        },
    ))
}

fn write_channel<'a>(ch: &Channel, output: &'a mut [u8]) -> Result<(&'a mut [u8], u64), GenError> {
    let (output, _) = write_str(&ch.name, output)?;
    let (output, _) = gen(le_u64(0), output)?;
    let (output, _) = write_enum::<ChannelType>(&ch.channel_type, output)?;
    let (output, _) = write_enum::<MsgFormat>(&ch.encoding_type, output)?;
    let (output, _) = write_str("", output)?; // payload
    let (output, _) = gen(le_u64(ch.addresses.len() as u64), output)?;
    let mut rest = output;
    for addr in &ch.addresses {
        let (output, _) = write_str(&urlunparse(addr.clone()), rest)?;
        rest = output;
    }
    Ok((rest, 0))
}

//
// Discovery Message
//

fn read_ds_msg(input: &[u8]) -> nom::IResult<&[u8], DiscoveryMessage> {
    let (input, _) = read_enum::<MsgFormat>(input)?;
    let (input, state) = read_enum::<DiscoveryState>(input)?;
    let (input, uri) = map(read_str, |v| urlparse(v))(input)?;
    let (input, channel_cnt) = read_usize(input)?;
    let mut channels = Vec::with_capacity(channel_cnt);
    let mut input = input;
    for _ in 0..channel_cnt {
        let (rest, channel) = read_channel(input)?;
        channels.push(channel);
        input = rest;
    }
    Ok((
        input,
        DiscoveryMessage {
            state,
            uri,
            channels,
        },
    ))
}

fn write_ds_msg<'a>(
    msg: &DiscoveryMessage,
    output: &'a mut [u8],
) -> Result<(&'a mut [u8], u64), GenError> {
    let (output, _) = write_enum::<MsgFormat>(&MsgFormat::Bincode, output)?;
    let (output, _) = write_enum::<DiscoveryState>(&msg.state, output)?;
    let (output, _) = write_str(&msg.uri.unparse(), output)?;
    let (output, sz) = gen(le_u64(msg.channels.len() as u64), output)?;
    let mut rest = output;
    let mut sz = sz;
    for ch in &msg.channels {
        let (output, _) = write_channel(&ch, rest)?;
        rest = output;
        sz = sz;
    }
    Ok((rest, sz))
}

pub async fn read_msg(
    mut stream: TcpStream,
) -> Result<DiscoveryMessage, Box<dyn std::error::Error>> {
    let sz = read_u32_async(&mut stream).await? as usize;
    let mut buf = SmallVec::<[u8; 1024]>::with_capacity(sz);
    buf.resize(sz, 0u8);
    stream.read_exact(&mut buf).await?;
    match read_ds_msg(buf.as_slice()) {
        Ok((_, msg)) => Ok(msg),
        Err(e) => Err(format!(
            "Invalid data during deserialisation from discovery msg {}",
            e
        )
        .into()),
    }
}

pub async fn write_msg(
    mut stream: TcpStream,
    msg: DiscoveryMessage,
) -> Result<(), Box<dyn std::error::Error>> {
    let mut buf = SmallVec::<[u8; 1024]>::new();
    buf.resize(1024, 0u8);
    let (remain, _) = write_ds_msg(&msg, buf.as_mut_slice())?;
    let sz = 1024 - remain.len();
    eprintln!("Sending msg {:?}", &msg);
    stream.write(&u32::to_le_bytes(sz as u32)).await?;
    stream.write_all(&buf[0..sz]).await?;
    Ok(())
}

//
// Smart monitor messages
//

fn read_sm_msg(input: &[u8]) -> nom::IResult<&[u8], MonitorMsg> {
    let (input, name) = read_str(input)?;
    let (input, date) = read_usize(input)?;
    let (input, time) = read_u32(input)?;
    let (input, msg_format) = read_enum::<MsgFormat>(input)?;
    let (input, payload) = read_enum::<Payload>(input)?;
    let (input, data) = match payload {
        Payload::Entry => {
            let rest = input;
            let (input, sz) = read_usize(rest)?;
            let (input, sz) = take(sz)(input)?;
            Ok((input, sz))
        }
        Payload::Exit => Ok((input, &[][..])),
    }?;
    Ok((
        input,
        MonitorMsg {
            channel_name: name.to_string(),
            adj_time_stamp: NaiveDateTime::from_timestamp(date as i64, time),
            msg_format,
            payload,
            data: data.to_vec(),
        },
    ))
}

fn write_sm_msg<'a>(
    msg: &MonitorMsg,
    output: &'a mut [u8],
) -> Result<(&'a mut [u8], u64), GenError> {
    let (output, _) = write_str(&msg.channel_name, output)?;
    let (output, _) = write_usize(
        msg.adj_time_stamp.timestamp_millis() as usize / 1000,
        output,
    )?;
    let (output, _) = write_u32(msg.adj_time_stamp.timestamp_subsec_nanos(), output)?;
    let (output, _) = write_enum::<MsgFormat>(&msg.msg_format, output)?;
    let (output, _) = write_enum::<Payload>(&msg.payload, output)?;
    if !msg.data.is_empty() {
        let (output, _) = write_usize(msg.data.len(), output)?;
        let (_, _) = gen(slice(&msg.data), output)?;
    }
    Ok((output, 0))
}

pub async fn read_sm_message(
    mut stream: TcpStream,
) -> Result<MonitorMsg, Box<dyn std::error::Error>> {
    let sz = read_u64_async(&mut stream).await? as usize;
    let mut buf = SmallVec::<[u8; 1024]>::with_capacity(sz);
    buf.resize(sz, 0u8);
    stream.read_exact(&mut buf).await?;
    match read_sm_msg(buf.as_slice()) {
        Ok((_, msg)) => Ok(msg),
        Err(e) => Err(format!(
            "Invalid data during deserialisation from smart monitoring msg {}",
            e
        )
        .into()),
    }
}

pub async fn write_sm_message(
    stream: &mut TcpStream,
    msg: MonitorMsg,
) -> Result<(), Box<dyn std::error::Error>> {
    let mut buf = SmallVec::<[u8; 1024]>::with_capacity(1024);
    let (remain, _) = write_sm_msg(&msg, buf.as_mut_slice())?;
    let sz = 1024 - remain.len();
    stream.write(&usize::to_le_bytes(sz)).await?;
    stream.write_all(&buf[0..sz]).await?;
    Ok(())
}

//
// Queue msg
//

fn read_queue_msg(input: &[u8]) -> nom::IResult<&[u8], QueueMessage> {
    let (input, name) = read_str(input)?;
    let (input, sz) = read_usize(input)?;
    let (input, data) = take(sz)(input)?;
    Ok((
        input,
        QueueMessage {
            channel_name: name.to_string(),
            data: data.to_vec(),
        },
    ))
}

fn write_queue_msg<'a>(
    msg: &QueueMessage,
    output: &'a mut [u8],
) -> Result<(&'a mut [u8], u64), GenError> {
    let (output, _) = write_str(&msg.channel_name, output)?;
    let (output, _) = write_usize(msg.data.len(), output)?;
    let (output, _) = gen(slice(&msg.data), output)?;
    Ok((output, 0))
}

pub async fn read_queue_message(
    mut stream: TcpStream,
) -> Result<QueueMessage, Box<dyn std::error::Error>> {
    let sz = read_u64_async(&mut stream).await? as usize;
    let mut buf = SmallVec::<[u8; 1024]>::with_capacity(sz);
    buf.resize(sz, 0u8);
    stream.read_exact(&mut buf).await?;
    match read_queue_msg(buf.as_slice()) {
        Ok((_, msg)) => Ok(msg),
        Err(e) => Err(format!("Invalid data during deserialisation from queue msg {}", e).into()),
    }
}

pub async fn write_queue_message(
    mut stream: TcpStream,
    msg: QueueMessage,
) -> Result<(), Box<dyn std::error::Error>> {
    let mut buf = SmallVec::<[u8; 1024]>::with_capacity(1024);
    write_queue_msg(&msg, buf.as_mut_slice())?;
    stream.write(&usize::to_le_bytes(buf.len())).await?;
    stream.write_all(&buf).await?;
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn protocol_discovery() {
        let msg = DiscoveryMessage {
            state: DiscoveryState::Connect,
            uri: urlparse("tcp://127.0.0.1:12345"),
            channels: vec![Channel {
                name: "mychannel".to_string(),
                instance: 0,
                channel_type: ChannelType::Input,
                encoding_type: MsgFormat::Bincode,
                addresses: vec![
                    urlparse("tcp://192.168.1.1:55126"),
                    urlparse("tcp://192.145.0.27:156"),
                ],
            }],
        };
        let mut buf = SmallVec::<[u8; 1024]>::with_capacity(1024);
        buf.resize(1024, 0u8);
        write_ds_msg(&msg, &mut buf).expect("Failed to serialise");
        let (remaining, msg_out) = read_ds_msg(&buf).expect("failed to deserialise");
        assert_eq!(msg, msg_out);
        assert_eq!(remaining.len(), 901);

        let mut buf = SmallVec::<[u8; 1024]>::with_capacity(1024);
        buf.resize(1024, 0u8);
        write_ds_msg(&msg_out, &mut buf).expect("Failed to serialise");
        let (_, msg_out) = read_ds_msg(&buf).expect("failed to deserialise");
        assert_eq!(msg, msg_out);
    }

    #[test]
    fn protocol_monitor() {
        let msg = MonitorMsg {
            channel_name: "channel1".to_string(),
            adj_time_stamp: NaiveDateTime::from_timestamp(124563, 345),
            msg_format: MsgFormat::Json,
            payload: Payload::Entry,
            data: b"abdgeriuger".to_vec(),
        };
        let mut buf = SmallVec::<[u8; 1024]>::with_capacity(1024);
        buf.resize(1024, 0u8);
        write_sm_msg(&msg, &mut buf).expect("Failed to serialise");
        let (remaining, msg_out) = read_sm_msg(&buf).expect("failed to deserialise");
        assert_eq!(msg, msg_out);
        assert_eq!(remaining.len(), 979);
        (&msg_out);
    }

    #[test]
    fn urlparser() {
        let url = "tcp://abmac.local:12345";
        let parsed = Url::parse(url);
        let s = parsed.unparse();
        assert_eq!(url, &s);
    }
}
