use crate::utils::*;
use async_std::net::{IpAddr, Ipv4Addr, Ipv6Addr, SocketAddr};
use rmp::{decode::*, encode::*};
use std::borrow::Cow;
use std::convert::TryFrom;
use std::io::Cursor;
use std::io::{Read, Write};

#[derive(Copy, Clone, PartialEq, Debug)]
pub enum DiscoveryState {
    Connect = 1,
    ConnectResponse = 2,
    QueueData = 3,
    Error = 255,
}

#[derive(PartialEq, Debug, Copy, Clone, PartialOrd, Ord, Eq)]
pub enum ChannelType {
    Input,
    Output,
}

#[derive(PartialEq, Debug, Copy, Clone, PartialOrd, Ord, Eq)]
pub struct Channel {
    pub name: usize,
    pub instance: usize,
    pub channel_type: ChannelType,
}

#[derive(PartialEq, Debug)]
pub struct DiscoveryMessage<'a> {
    pub state: DiscoveryState,
    pub server: SocketAddr,
    pub channels: Vec<Cow<'a, Channel>>,
}

//

fn discovery_from_u8(byte: u8) -> DiscoveryState {
    match byte {
        1 => DiscoveryState::Connect,
        2 => DiscoveryState::ConnectResponse,
        3 => DiscoveryState::QueueData,
        _ => DiscoveryState::Error,
    }
}

fn channel_type_from_u8(byte: u8) -> ChannelType {
    match byte {
        0 => ChannelType::Input,
        _ => ChannelType::Output,
    }
}

impl Channel {
    pub fn with_str_name(name: &str, instance: usize, channel_type: ChannelType) -> Self {
        Self {
            name: str_radix(name),
            instance,
            channel_type,
        }
    }

    fn with_byte_name(name: usize, instance: usize, channel_type: ChannelType) -> Self {
        Self {
            name,
            instance,
            channel_type,
        }
    }
}

impl<'a> DiscoveryMessage<'a> {
    pub fn new(state: DiscoveryState, server: SocketAddr, channels: Vec<Cow<'a, Channel>>) -> Self {
        Self {
            state,
            server,
            channels,
        }
    }
}

fn as_u8_slice<T>(v: &[T]) -> &[u8] {
    unsafe {
        std::slice::from_raw_parts(v.as_ptr() as *const u8, v.len() * std::mem::size_of::<T>())
    }
}

fn write_socket<W: Write>(mut buf: W, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
    match addr.ip() {
        IpAddr::V4(ip) => {
            write_u8(&mut buf, 1)?;
            write_u32(&mut buf, ip.into())?;
        }
        IpAddr::V6(ip) => {
            write_u8(&mut buf, 2)?;
            let byte_ip: u128 = ip.into();
            write_u64(&mut buf, (byte_ip & 0xFFFFFFFF) as u64)?;
            write_u64(&mut buf, (byte_ip & 0xFFFFFFFF00000000) as u64)?;
        }
    }
    write_u16(&mut buf, addr.port())?;
    Ok(())
}

fn read_socket<R: Read>(mut r: R) -> Result<SocketAddr, Box<dyn std::error::Error>> {
    let sz = read_u8(&mut r)?;
    let addr = match sz {
        1 => {
            let ip = read_u32(&mut r)?;
            let port = read_u16(&mut r)?;
            SocketAddr::new(IpAddr::V4(Ipv4Addr::from(ip)), port)
        }
        _ => {
            let ip = read_u64(&mut r)? as u128;
            let ip = ip & ((read_u64(&mut r)? << 16) as u128);
            let port = read_u16(&mut r)?;
            SocketAddr::new(IpAddr::V6(Ipv6Addr::from(ip)), port)
        }
    };
    Ok(addr)
}
impl<'a> TryFrom<&'a DiscoveryMessage<'a>> for Vec<u8> {
    type Error = Box<dyn std::error::Error>;
    fn try_from(msg: &DiscoveryMessage) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
        let buf = Vec::with_capacity(100);
        let mut writer = Cursor::new(buf);
        write_u8(&mut writer, msg.state as u8)?;
        write_socket(&mut &mut writer, msg.server)?;
        write_u64(&mut writer, msg.channels.len() as u64)?;
        let _: Result<(), Box<dyn std::error::Error>> =
            msg.channels.iter().try_for_each(|channel| {
                write_u64(&mut writer, channel.name as u64)?;
                write_u64(&mut writer, channel.instance as u64)?;
                write_u8(&mut writer, channel.channel_type as u8)?;
                Ok(())
            });
        Ok(writer.into_inner())
    }
}

impl<'a> TryFrom<&[u8]> for DiscoveryMessage<'a> {
    type Error = Box<dyn std::error::Error>;
    fn try_from(bytes: &[u8]) -> Result<DiscoveryMessage<'a>, Box<dyn std::error::Error>> {
        let mut reader = Cursor::new(bytes);
        let state = discovery_from_u8(read_u8(&mut reader)?);
        let server = read_socket(&mut reader)?;
        let channel_sz = read_u64(&mut reader)?;
        let channels = (0..channel_sz)
            .map(|_| {
                let name = read_u64(&mut reader)? as usize;
                let instance = read_u64(&mut reader)? as usize;
                let c_type = read_u8(&mut reader)?;
                Ok(Cow::Owned(Channel::with_byte_name(
                    name,
                    instance,
                    channel_type_from_u8(c_type),
                )))
            })
            .collect::<Result<Vec<Cow<'a, Channel>>, Box<dyn std::error::Error>>>()?;
        Ok(DiscoveryMessage {
            state,
            server,
            channels,
        })
    }
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn two_way() {
        let discovery = DiscoveryMessage {
            state: DiscoveryState::Connect,
            server: SocketAddr::new(IpAddr::V4(Ipv4Addr::new(213, 123, 23, 5)), 31),
            channels: vec![
                Cow::Owned(Channel::with_str_name("mychannel", 13, ChannelType::Output)),
                Cow::Owned(Channel::with_str_name("mychannel2", 13, ChannelType::Input)),
            ],
        };
        let bytes: Vec<u8> = Vec::<u8>::try_from(&discovery).expect("Could not serialise");
        let recover = DiscoveryMessage::try_from(bytes.as_slice()).expect("Failed to serialise");
        assert_eq!(discovery, recover);
    }

    #[test]
    fn radix() {
        let s = "mychannel";
        let num = str_radix(&s);
        let s2 = radix_str(num as u64);
        assert_eq!(s, &s2);
    }
}
