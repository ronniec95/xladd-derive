using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using AARC.Utilities;

namespace AARC.Mesh.Model
{
    public partial class MeshMessage : IMeshMessage
    {
        public enum States
        {
            MessageIn = 0,
            MessageOut = 1,
            NTPMode = 2,
            ONConnect = 3,
            ERROR = 255
        };
        /// <summary>
        /// GraphId of zero means system message
        /// </summary>
        public UInt64 DateTimeTotalSeconds { get; set; }
        public UInt32 DateTimeMS { get; set; }
        public States State { get; set; }
        // JSON or MessagePack or Binary
        public byte EncodingType { get; set; }
        public UInt32 GraphId { get; set; }
        public UInt32 XId { get; set; }
        public Uri Service { get; set; }
        public string Channel { get; set; }
        public string PayLoad { get; set; }
        public IEnumerable<Uri> Routes { get; set; }
    }


    public partial class MeshMessage : IMeshMessage
    {
        public override string ToString() => Service.AbsoluteUri;

        public byte[] Encode(byte msgType)
        {
            (this.DateTimeTotalSeconds, this.DateTimeMS) = DateTimeUtilities.DateTimeToUnixTotalSeconds(DateTime.Now);
            //this.DateTimeTotalSeconds = Date
            var bytes = new List<byte>();
            // Date Time in Seconds from 1970
            bytes.AddRange(BitConverter.GetBytes(this.DateTimeTotalSeconds));
            // The milliseconds part of the date time from 1970
            bytes.AddRange(BitConverter.GetBytes(this.DateTimeMS));
            // Message State
            var state = (byte)(this.State == States.MessageIn ? States.MessageOut : this.State);
            bytes.Add(state);
            // EncodingType
            bytes.Add(this.EncodingType);
            // GraphId
            bytes.AddRange(BitConverter.GetBytes(this.GraphId));
            // Xid
            bytes.AddRange(BitConverter.GetBytes(this.XId));
            // Service
            bytes.AddRange(this.Service.AbsoluteUri.EncodeBytes());
            // Channel
            bytes.AddRange(this.Channel.EncodeBytes());
            // PayLoad
            var compressedbytes = AARC.Compression.Compression.CompressString(this.PayLoad);
            bytes.AddRange(compressedbytes);
            return bytes.ToArray();
        }

        IMeshMessage IMeshMessage.Decode(byte[] bytes) => Decode(bytes);
        
        public MeshMessage Decode(byte[] bytes)
        {
            // GraphId
            var msgPtr = 0;
            // Date Time in seconds
            this.DateTimeTotalSeconds = bytes.ToUInt64(ref msgPtr);
            // Plus the ms bit
            this.DateTimeMS = bytes.ToUInt32(ref msgPtr);
            // States Type
            var state = bytes[++msgPtr];
            this.State = States.MessageIn;
            // Encoding Type
            this.EncodingType = bytes[msgPtr++];
            // Feature
            this.GraphId = bytes.ToUInt32(ref msgPtr);
            // Xid
            this.XId = bytes.ToUInt32(ref msgPtr);
            // Service
            this.Service = new Uri(bytes.DecodeString(ref msgPtr));
            // Channel Alias
            this.Channel = bytes.DecodeString(ref msgPtr);
            // PayLoad
            this.PayLoad = AARC.Compression.Compression.DecompressString(bytes, msgPtr);
            return this;
        }
    }
}