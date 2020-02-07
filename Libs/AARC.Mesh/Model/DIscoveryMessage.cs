using System;
using System.Collections.Generic;
using System.IO;
using AARC.Mesh.Interface;
using MsgPack;
using MsgPack.Serialization;

namespace AARC.Mesh.Model
{
    public partial class DiscoveryMessage : IMeshMessage
    {
        public enum DiscoveryStates { Connect = 0, Register = 1, GetInputQs = 2, GetOutputQs = 3, Error = 255 };

        public DiscoveryStates State { get; set; }
        public int Port { get; set; }
        public string HostServer { get; set; }
        public string Payload { get; set; }
        public IEnumerable<string> Routes => throw new NotImplementedException();
    }

    public partial class DiscoveryMessage : IMeshMessage
    {
        public byte[] Encode(byte msgType)
        {
            if (msgType == 0)
                EncodeDisoveryMessageType0(this);
            else if (msgType == 1)
                EncodeDisoveryMessageType1(this);
            throw new NotSupportedException();
        }

        protected static byte[] EncodeDisoveryMessageType0(DiscoveryMessage dm)
        {
            var bytes = new List<byte>();
            // MessageTypes (byte)
            // 0 = Simple
            // 1 = MsgPak
            bytes.Add((byte)0);
            // State (8bits)
            bytes.Add((byte)dm.State);
            // Port (16bits)
            bytes.AddRange(BitConverter.GetBytes((UInt16)dm.Port));
            // HostService len (8bits)
            bytes.Add((byte)dm.HostServer.Length);
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(dm.HostServer));
            // PayLoad len (64bits)
            if (string.IsNullOrEmpty(dm.Payload))
                bytes.AddRange(BitConverter.GetBytes((UInt64)0));
            else
            {
                bytes.AddRange(BitConverter.GetBytes((UInt64)dm.Payload.Length));
                bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(dm.Payload));
            }
            return bytes.ToArray();
        }

        IMeshMessage IMeshMessage.Decode(byte[] bytes)
        {
            return Decode(bytes);
        }

        public DiscoveryMessage Decode(byte[] bytes)
        {
            // Message Type
            // 0 = Simple
            // 1 = MsgPak

            var msgType = bytes[0];

            if (msgType == 0)
                return DecodeDiscoveryMessageType0(bytes);
            else if (msgType == 1)
                return DecodeDiscoveryMessageType1(bytes);

            throw new NotSupportedException();
        }

        protected DiscoveryMessage DecodeDiscoveryMessageType0(byte[] bytes)
        {
            int msgPtr = 0;
            // 1 bytes state
            // 2 bytes for port
            // 1 bytes string len host
            // len bytes string
            // 4 bytes for len payload
            // payload

            var state = bytes[++msgPtr];
            State = (DiscoveryStates)state;
            // Port
            Port = BitConverter.ToUInt16(bytes, msgPtr);
            msgPtr += sizeof(UInt16);
            // Host Service
            int len = bytes[msgPtr];
            msgPtr += 1;
            this.HostServer = System.Text.Encoding.ASCII.GetString(bytes, msgPtr, len);
            msgPtr += len;
            // PayLoad
            var payloadLen = BitConverter.ToUInt64(bytes, msgPtr);
            if (payloadLen > 0) // PayLoad is allowed to be empty
            {
                msgPtr += sizeof(UInt64);
                Payload = System.Text.Encoding.ASCII.GetString(bytes, msgPtr, (int)payloadLen);
            }
            return this;
        }

        protected static MessagePackSerializer<DiscoveryMessage> _msgPackSerializer = MessagePackSerializer.Get<DiscoveryMessage>();
        protected DiscoveryMessage DecodeDiscoveryMessageType1(byte[] bytes)
        {
            using (var byteUnpacker = Unpacker.Create(bytes, 1))
                return _msgPackSerializer.UnpackFrom(byteUnpacker);
        }

        protected byte[] EncodeDisoveryMessageType1(DiscoveryMessage dm)
        {
            var stream = new MemoryStream();
            stream.WriteByte(1);

            _msgPackSerializer.Pack(stream, dm);

            return stream.ToArray();
        }
    }
}