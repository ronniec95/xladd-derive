using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AARC.Mesh.Model
{
    public partial class MeshMessage : IMeshMessage
    {
        /// <summary>
        /// GraphId of zero means system message
        /// </summary>
        public uint GraphId { get; set; }
        public uint XId { get; set; }
        public string Service { get; set; }
        public string QueueName { get; set; }
        public string PayLoad { get; set; }
        public bool IsValid() => !string.IsNullOrWhiteSpace(Service) && !string.IsNullOrWhiteSpace(QueueName) && XId > 0;
        public IEnumerable<string> Routes { get; set; }

        public byte[] Serialize()
        {
#if OBS
            var json =JsonConvert.SerializeObject(this);
            return System.Text.Encoding.ASCII.GetBytes(json);
#else
            var bytes = this.Encode();
            return bytes;
#endif
        }
        public static MeshMessage Deserialise(byte[] bytes)
        {
#if OBS
            var message = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);
            return JsonConvert.DeserializeObject<MeshMessage>(message);
#else
            var m = new MeshMessage();
            m.Decode(bytes);
            return m;
#endif
        }
    }


    public partial class MeshMessage : IMeshMessage
    {
        public override string ToString() => Service;

        public byte[] Encode()
        {
            var bytes = new List<byte>();
            // GraphId
            bytes.AddRange( BitConverter.GetBytes(this.GraphId));
            // Xid
            bytes.AddRange(BitConverter.GetBytes(this.XId));
            // Service
            bytes.AddRange(BitConverter.GetBytes(this.Service.Length));      
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(this.Service));
            // QueueName
            bytes.AddRange(BitConverter.GetBytes(this.QueueName.Length));
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(this.QueueName));
            // PayLoad
            if (string.IsNullOrEmpty(this.PayLoad))
                bytes.AddRange(BitConverter.GetBytes((uint)0));
            else
            {
                bytes.AddRange(BitConverter.GetBytes(this.PayLoad.Length));
                bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(this.PayLoad));
            }
            return bytes.ToArray();
        }

        IMeshMessage IMeshMessage.Decode(byte[] bytes) => Decode(bytes);
        
        public MeshMessage Decode(byte[] bytes)
        {
            // GraphId
            var msgPtr = 0;
            this.GraphId = BitConverter.ToUInt32(bytes, msgPtr);
            msgPtr += sizeof(uint);
            // Xid
            this.XId = BitConverter.ToUInt32(bytes, msgPtr);
            msgPtr += sizeof(uint);
            // Service
            var len = BitConverter.ToInt32(bytes, msgPtr);
            msgPtr += sizeof(Int32);
            this.Service = System.Text.Encoding.ASCII.GetString(bytes, msgPtr, len);
            msgPtr += len;
            // QueueName
            len = BitConverter.ToInt32(bytes, msgPtr);
            msgPtr += sizeof(Int32);
            this.QueueName = System.Text.Encoding.ASCII.GetString(bytes, msgPtr, len);
            msgPtr += len;
            // PayLoad
            len = BitConverter.ToInt32(bytes, msgPtr);
            if (len > 0) // PayLoad is allowed to be empty
            {
                msgPtr += sizeof(Int32);
                this.PayLoad = System.Text.Encoding.ASCII.GetString(bytes, msgPtr, len);
            }
            return this;
        }

        public static MeshMessage DeserializeMeshMessage(byte[] bytes, ILogger _logger = null)
        {
            // Zero bytes is a keep alive message
            if (bytes.Length == 0)
                return null;

            _logger?.LogDebug($"MSM Rx Received bytes {bytes.Length}");
            //var data = System.Text.Encoding.ASCII.GetString(bytes, 0, bytes.Length);

            //if (data.ValidateJSON())
            {
                var message = MeshMessage.Deserialise(bytes);
                if (!message.IsValid())
                {
                    _logger?.LogWarning($"MSM Invalid message");
                    if (message.GraphId == 0)
                        _logger?.LogWarning($"System Message GraphId");
                    else
                        _logger?.LogInformation($"GraphId {message.GraphId} OK");

                    if (message.XId > 0)
                        _logger?.LogWarning($"Missing XId");
                    else
                        _logger?.LogInformation($"XId {message.XId} OK");

                    if (string.IsNullOrWhiteSpace(message.Service))
                        _logger?.LogWarning($"Missing Service");
                    else
                        _logger?.LogInformation($"Service {message.Service} OK");
                    if (string.IsNullOrWhiteSpace(message.QueueName))
                        _logger?.LogWarning($"Missing Queue Name");
                    else
                        _logger?.LogInformation($"Action {message.QueueName} OK");

                    _logger?.LogInformation($"PayLoad {message.PayLoad}");
                }
                else return message;
            }
            //else
            //    _logger?.LogWarning($"Bad format {data}");
            return null;
        }
    }
}