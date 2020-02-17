use crate::messages::*;
use crate::net_reader::*;
use async_std::net::SocketAddr;
use num_derive::ToPrimitive;
use num_traits::FromPrimitive;
use std::borrow::Cow;
use std::convert::TryFrom;
use std::io::Cursor;
use std::io::{Read, Write};
use std::net::ToSocketAddrs;

fn write_socket<W: Write>(mut buf: W, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
    let addr = addr.to_string();
    let addr = addr.as_str();
    let ch = ChannelId::from(addr);
    for byte in ch.as_bytes() {
        write_u8(&mut buf, byte)?;
    }
    Ok(())
}
// fn write_socket<W: Write>(mut buf: W, addr: SocketAddr) -> Result<(), Box<dyn std::error::Error>> {
//     match addr.ip() {
//         IpAddr::V4(ip) => {
//             write_u8(&mut buf, 1u8)?;
//             write_u32(&mut buf, ip.into())?;
//         }
//         IpAddr::V6(ip) => {
//             write_u8(&mut buf, 2u8)?;
//             let byte_ip: u128 = ip.into();
//             write_u64(&mut buf, (byte_ip & 0xFFFFFFFF) as u64)?;
//             write_u64(&mut buf, (byte_ip & 0xFFFFFFFF00000000) as u64)?;
//         }
//     }
//     write_u16(&mut buf, addr.port())?;
//     Ok(())
// }

// fn read_socket<R: Read>(mut r: R) -> Result<SocketAddr, Box<dyn std::error::Error>> {
//     let sz = read_u8(&mut r)?;
//     let addr = match sz {
//         1 => {
//             let ip = read_u32(&mut r)?;
//             let port = read_u16(&mut r)?;
//             SocketAddr::new(IpAddr::V4(Ipv4Addr::from(ip)), port)
//         }
//         _ => {
//             let ip = read_u64(&mut r)? as u128;
//             let ip = ip & ((read_u64(&mut r)? << 16) as u128);
//             let port = read_u16(&mut r)?;
//             SocketAddr::new(IpAddr::V6(Ipv6Addr::from(ip)), port)
//         }
//     };
//     Ok(addr)
// }

fn read_socket<R: Read>(mut r: R) -> Result<SocketAddr, Box<dyn std::error::Error>> {
    let sz = read_u8(&mut r)?;
    let mut v = Vec::with_capacity(sz as usize); // static vec
    for _ in 0..sz {
        v.push(read_u64(&mut r)? as usize);
    }
    let channel_id = ChannelId::new(v);
    let addr = String::from(&channel_id);
    let addr = if addr.starts_with("tcp://") {
        &addr[6..]
    } else {
        &addr[..]
    };
    let mut addr = addr.to_socket_addrs()?;
    Ok(addr.next().unwrap())
}

impl<'a> TryFrom<&'a DiscoveryMessage<'a>> for Vec<u8> {
    type Error = Box<dyn std::error::Error>;
    fn try_from(msg: &DiscoveryMessage) -> Result<Vec<u8>, Box<dyn std::error::Error>> {
        let buf = Vec::with_capacity(100);
        let mut writer = Cursor::new(buf);
        write_u8(&mut writer, 0u8)?;
        write_u8(&mut writer, msg.state as u8)?;
        write_socket(&mut &mut writer, msg.server)?;
        write_u64(&mut writer, msg.channels.len() as u64)?;
        let _: Result<(), Box<dyn std::error::Error>> =
            msg.channels.iter().try_for_each(|channel| {
                channelid_to_writer(&mut writer, &channel.name)?;
                write_u64(&mut writer, channel.instance as u64)?;
                write_u8(&mut writer, channel.channel_type as u8)?;
                write_u8(&mut writer, channel.encoding_type as u8)?;
                write_u8(&mut writer, 0u8)?; // payload
                write_u64(&mut writer, channel.addresses.len() as u64)?;
                for address in &channel.addresses {
                    write_socket(&mut &mut writer, **address)?;
                }
                Ok(())
            });
        Ok(writer.into_inner())
    }
}

impl<'a> TryFrom<&[u8]> for DiscoveryMessage<'a> {
    type Error = Box<dyn std::error::Error>;
    fn try_from(bytes: &[u8]) -> Result<DiscoveryMessage<'a>, Box<dyn std::error::Error>> {
        let mut reader = Cursor::new(bytes);
        let _ = read_u8(&mut reader)?;
        let state: DiscoveryState = FromPrimitive::from_u8(read_u8(&mut reader)?).unwrap();
        let server = read_socket(&mut reader)?;
        let channel_sz = read_u64(&mut reader)?;
        let channels = (0..channel_sz)
            .map(|_| {
                let channel_id = channelid_from_reader(&mut reader)?;
                let instance = read_u64(&mut reader)? as usize;
                let c_type: ChannelType = FromPrimitive::from_u8(read_u8(&mut reader)?).unwrap();
                let m_type: MsgFormat = FromPrimitive::from_u8(read_u8(&mut reader)?).unwrap();
                let _ = channelid_from_reader(&mut reader)?;
                let num_address = read_u64(&mut reader)?;
                let addresses = (0..num_address)
                    .map(|_| Cow::Owned(read_socket(&mut reader).unwrap()))
                    .collect::<Vec<Cow<SocketAddr>>>();
                Ok(Channel {
                    name: channel_id,
                    instance,
                    channel_type: c_type,
                    encoding_type: m_type,
                    addresses,
                })
            })
            .collect::<Result<Vec<Channel>, Box<dyn std::error::Error>>>()?;
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
    use std::net::{IpAddr, Ipv4Addr};
    #[test]
    fn two_way() {
        let discovery = DiscoveryMessage {
            state: DiscoveryState::Connect,
            server: SocketAddr::new(IpAddr::V4(Ipv4Addr::new(213, 123, 23, 5)), 31),
            channels: vec![
                Channel::new(
                    ChannelId::from("mychannel"),
                    13,
                    ChannelType::Output,
                    MsgFormat::Bincode,
                ),
                Channel::new(
                    ChannelId::from("mychannel2"),
                    13,
                    ChannelType::Output,
                    MsgFormat::Bincode,
                ),
            ],
        };
        let bytes: Vec<u8> = Vec::<u8>::try_from(&discovery).expect("Could not serialise");
        let recover = DiscoveryMessage::try_from(bytes.as_slice()).expect("Failed to serialise");
        assert_eq!(discovery, recover);
    }

    #[test]
    fn radix() {
        let s = "abmac.local:0";
        let num = ChannelId::from(s);
        dbg!(&num);
        let s2 = String::from(&num);
        assert_eq!(s2, "abmac.local:0");
    }

    use std::net::ToSocketAddrs;
    #[test]
    fn lookup() {
        let ips = "abmac.local:80"
            .to_socket_addrs()
            .expect("failed to resolve ip");
        dbg!(&ips);
    }
}
