using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

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
        public byte[] Encode()
        {
            var bytes = new List<byte>();
            // State (8bits)
            bytes.Add((byte)this.State);
            // Port (16bits)
            bytes.AddRange(BitConverter.GetBytes((UInt16)this.Port));
            // HostService len (8bits)
            bytes.Add((byte)this.HostServer.Length);
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(this.HostServer));
            // PayLoad len (64bits)
            if (string.IsNullOrEmpty(this.Payload))
                bytes.AddRange(BitConverter.GetBytes((UInt64)0));
            else
            {
                bytes.AddRange(BitConverter.GetBytes((UInt64)this.Payload.Length));
                bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(this.Payload));
            }
            return bytes.ToArray();
        }

        IMeshMessage IMeshMessage.Decode(byte[] bytes)
        {
            return Decode(bytes);
        }

        public DiscoveryMessage Decode (byte[] bytes)
        {
            // 1 bytes state
            // 2 bytes for port
            // 1 bytes string len host
            // len bytes string
            // 4 bytes for len payload
            // payload

            var state = bytes[0];
            State = (DiscoveryStates)state;
            int msgPtr = 1;
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
    }
}