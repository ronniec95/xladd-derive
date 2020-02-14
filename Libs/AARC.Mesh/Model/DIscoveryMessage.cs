using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh.Model
{
    public struct MeshChannel
    {
        public enum ChannelTypes
        {
            Input = 0,
            Output = 1,
        };

        // Channel Name
        public string Name { get; set; }
        // Clustering feature
        public UInt64 Instance { get; set; }
        // Input or Output channels
        public ChannelTypes ChannelType { get; set; }
        // JSON or MessagePack or Binary
        public byte EncodingType { get; set; }
        // Business type discription like string, int or Dictionary
        public string PayloadType { get; set; }
        // List of MS address
        public HashSet<Uri> Addresses { get; set; }
    }

    public partial class DiscoveryMessage : IMeshMessage
    {
        public enum DiscoveryStates
        {
            Connect = 1,
            ConnectResponse = 2,
            ChannelData = 3,
            Error = 255,
        };

        public DiscoveryStates State { get; set; }
        public Uri Service { get; set; }
        public List<MeshChannel> Channels { get; set; }
        public IEnumerable<Uri> Routes => throw new NotImplementedException();
    }

    public partial class DiscoveryMessage : IMeshMessage
    {
        public byte[] Encode(byte msgType)
        {
            if (msgType == 0)
                return EncodeDisoveryMessageType0(this);

            throw new NotSupportedException();
        }

        /// <summary>
        /// DiscoveryMessage Format
        /// (byte) MsgType
        /// (byte) State
        /// (Radix.Encode) for Service
        /// (byte) No Channels
        ///     (byte) ChannelType
        ///     (bytes) Instance
        ///     (Radix.Encode) Name
        /// </summary>
        /// <param name="dm"></param>
        /// <returns></returns>
        protected static byte[] EncodeDisoveryMessageType0(DiscoveryMessage dm)
        {
            var bytes = new List<byte>();
            // MessageTypes (byte)
            // 0 = Simple
            // 1 = MsgPak
            bytes.Add((byte)0);
            // State (8bits)
            bytes.Add((byte)dm.State);
            // Service to bytes
            bytes.AddRange(dm.Service.AbsoluteUri.EncodeBytes());

            bytes.AddRange(BitConverter.GetBytes((UInt64)(dm.Channels?.Count ?? 0)));
            if (dm.Channels?.Count > 0)
                foreach (var channel in dm.Channels)
                {
                    // Channel Name - string to bytes
                    bytes.AddRange(channel.Name.EncodeBytes());

                    bytes.AddRange(BitConverter.GetBytes(channel.Instance));
                    bytes.Add((byte)channel.ChannelType);
                    bytes.Add(channel.EncodingType);
                    var payloadRadix = Radix.Encode(channel.PayloadType);
                    Radix.RadixToBytes(bytes, payloadRadix);
                    // Add addresses
                    bytes.AddRange(BitConverter.GetBytes((UInt64)(channel.Addresses?.Count ?? 0)));
                    foreach (var address in channel.Addresses)
                    {
                        // address to bytes
                        bytes.AddRange(address.AbsoluteUri.EncodeBytes());
                    }
                }
            return bytes.ToArray();
        }

        IMeshMessage IMeshMessage.Decode(byte[] bytes) => Decode(bytes);

        public DiscoveryMessage Decode(byte[] bytes)
        {
            // Message Type
            // 0 = Simple
            // 1 = MsgPak
            try
            {
                var msgType = bytes[0];

                if (msgType == 0)
                    return DecodeDiscoveryMessageType0(bytes);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            throw new NotSupportedException();
        }

        /// <summary>
        /// DiscoveryMessage Format
        /// (byte) MsgType
        /// (byte) State
        /// (Radix.Decode) for Service
        /// (byte) No Channels
        ///     (bytes Radix) Name
        ///     (byte) ChannelType
        ///     (bytes) Instance
        ///     (byte) Encoding
        ///     (Radix) PayloadFormat
        ///     (byte) Addresses length
        ///     (Radix.Decode) Addresses
        /// </summary>
        /// <param name="dm"></param>
        /// <returns></returns>
        protected DiscoveryMessage DecodeDiscoveryMessageType0(byte[] bytes)
        {
            int msgPtr = 0;

            var state = bytes[++msgPtr];
            State = (DiscoveryStates)state;
            ++msgPtr;

            // Service
            var service = bytes.DecodeString(ref msgPtr);
            this.Service = new Uri(service);

            var noChannels = BitConverter.ToUInt64(bytes, msgPtr);
            msgPtr += sizeof(UInt64);
            if (noChannels > 0)
            {
                this.Channels = new List<MeshChannel>();
                for (var c = (UInt64)0; c < noChannels; c++)
                {
                    var channel = new MeshChannel
                    {
                        Name = bytes.DecodeString(ref msgPtr),
                        Instance = BitConverter.ToUInt64(bytes, msgPtr)
                    };
                    msgPtr += sizeof(UInt64);

                    channel.ChannelType = (MeshChannel.ChannelTypes)bytes[msgPtr++];
                    channel.EncodingType = bytes[msgPtr++];
                    var payloadRadix = Radix.BytesToRadix(bytes, ref msgPtr);
                    channel.PayloadType = Radix.Decode(payloadRadix);
                    var noAddresses = BitConverter.ToUInt64(bytes, msgPtr);
                    msgPtr += sizeof(UInt64);
                    if (noAddresses > 0)
                    {
                        channel.Addresses = new HashSet<Uri>();
                        for (var i = (UInt64)0; i < noAddresses; ++i)
                        {
                            var uri = new Uri(bytes.DecodeString(ref msgPtr));
                            channel.Addresses.Add(uri);
                        }
                    }

                    this.Channels.Add(channel);
                }
            }
            return this;
        }
    }
}