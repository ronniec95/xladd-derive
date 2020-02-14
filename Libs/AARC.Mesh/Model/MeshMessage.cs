using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh.Model
{
    public partial class MeshMessage : IMeshMessage
    {
        /// <summary>
        /// GraphId of zero means system message
        /// </summary>
        public uint GraphId { get; set; }
        public uint XId { get; set; }
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
            var bytes = new List<byte>  { (byte)0 };
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
            //                bytes.AddRange(BitConverter.GetBytes(compressedbytes.Length));
            bytes.AddRange(compressedbytes);
            return bytes.ToArray();
        }

        IMeshMessage IMeshMessage.Decode(byte[] bytes) => Decode(bytes);
        
        public MeshMessage Decode(byte[] bytes)
        {
            // GraphId
            var msgPtr = 0;
            var msgType = bytes[msgPtr++];
            if (msgType != 0) throw new NotSupportedException();

            this.GraphId = BitConverter.ToUInt32(bytes, msgPtr);
            msgPtr += sizeof(uint);
            // Xid
            this.XId = BitConverter.ToUInt32(bytes, msgPtr);
            msgPtr += sizeof(uint);
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