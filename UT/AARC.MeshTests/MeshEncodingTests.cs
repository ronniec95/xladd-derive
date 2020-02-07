using System;
using System.Buffers;
using AARC.Mesh.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AARC.MeshTests
{
    [TestClass]
    public class MeshEncodingTests
    {
        public MeshEncodingTests()
        {
        }

        [TestMethod]
        public void TestEncodeWithPayLoad()
        {
            var test = new MeshMessage
            {
                GraphId = 9999,
                XId = 6666,
                Service = "TestService",
                Channel = "TestQueue",
                PayLoad = "TestPayLoad"
            };

            var bytes = test.Encode(0);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual<int>(0, bytes.Length);
        }

        [TestMethod]
        public void TestEncodeNullPayLoad()
        {
            var test = new MeshMessage
            {
                GraphId = 9999,
                XId = 6666,
                Service = "TestService",
                Channel = "TestQueue",
                PayLoad = null
            };

            var bytes = test.Encode(0);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual<int>(0, bytes.Length);
        }

        [TestMethod]
        public void TestDecodeWIthPayLoad()
        {
            var test = new MeshMessage
            {
                GraphId = 9999,
                XId = 6666,
                Service = "TestService",
                Channel = "TestQueue",
                PayLoad = "TestPayLoad"
            };

            var bytes = test.Encode(0);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual<int>(0, bytes.Length);

            var test2 = new MeshMessage();
            test2.Decode(bytes);
            Assert.IsNotNull(test2);

            Assert.AreEqual<uint>(test.GraphId, test2.GraphId);
            Assert.AreEqual<uint>(test.XId, test2.XId);
            Assert.AreEqual<string>(test.Service, test2.Service);
            Assert.AreEqual<string>(test.Channel, test2.Channel);
            Assert.AreEqual<string>(test.PayLoad, test2.PayLoad);
        }

        public void TestDecodeNulPayLoad()
        {
            var test = new MeshMessage
            {
                GraphId = 9999,
                XId = 6666,
                Service = "TestService",
                Channel = "TestQueue",
                PayLoad = null
            };

            var bytes = test.Encode(0);
            Assert.IsNotNull(bytes);
            Assert.AreNotEqual<int>(0, bytes.Length);
            var test2 = new MeshMessage();
            test2.Decode(bytes);
            Assert.IsNotNull(test2);

            Assert.AreEqual<uint>(test.GraphId, test2.GraphId);
            Assert.AreEqual<uint>(test.XId, test2.XId);
            Assert.AreEqual<string>(test.Service, test2.Service);
            Assert.AreEqual<string>(test.Channel, test2.Channel);
            Assert.AreEqual<string>(test.PayLoad, test2.PayLoad);
        }

        [TestMethod]
        public void TestMeshMessageDecodeReadonlySequence()
        {
            var m = new MeshMessage { GraphId = 0, XId = 1, Channel = "channel1", Service = "localhost:1234", PayLoad = "[]" };
            var bmsg = m.Encode(0);
            var refmsgLen = bmsg.Length;
            var bytes = PacketProtocol.WrapMessage(bmsg);

            var sequence = new ReadOnlySequence<byte>(bytes);
            var bmsgLen = sequence.Slice(0, 4).ToArray();
            var msgLen = BitConverter.ToInt32(bmsgLen);
            Assert.AreEqual(refmsgLen, msgLen);
            var rawmsg = sequence.Slice(4, msgLen).ToArray();
            var mm = new MeshMessage();
            mm.Decode(rawmsg);
            Assert.AreEqual(m.GraphId, mm.GraphId);
            Assert.AreEqual(m.XId, mm.XId);
            Assert.AreEqual(m.Service, mm.Service);
            Assert.AreEqual(m.Channel, mm.Channel);
            Assert.AreEqual(m.PayLoad, mm.PayLoad);
        }

        public MeshMessage Decode(ReadOnlySequence<byte> source)
        {
            var bmsgLen = source.Slice(0, 4).ToArray();
            var m = new MeshMessage();
            if (source.IsSingleSegment)
            {
                var msgLen = BitConverter.ToInt32(bmsgLen);
                var rawmsg = source.Slice(4, msgLen).ToArray();

                m.Decode(rawmsg);
            }
            else
            {
                // GraphId
                var msgPtr = 0;
                m.GraphId = BitConverter.ToUInt32(source.Slice(msgPtr, 4).ToArray());
                msgPtr += sizeof(uint);
                // Xid
                m.XId = BitConverter.ToUInt32(source.Slice(msgPtr, 4).ToArray());
                msgPtr += sizeof(uint);
                // Service
                var len = BitConverter.ToInt32(source.Slice(msgPtr, 4).ToArray());
                msgPtr += sizeof(Int32);
                m.Service = System.Text.Encoding.ASCII.GetString(source.Slice(msgPtr, len).ToArray());
                msgPtr += len;
                // QueueName
                len = BitConverter.ToInt32(source.Slice(msgPtr, 4).ToArray());
                msgPtr += sizeof(Int32);
                m.Channel = System.Text.Encoding.ASCII.GetString(source.Slice(msgPtr, len).ToArray());
                msgPtr += len;
                // PayLoad
                len = BitConverter.ToInt32(source.Slice(msgPtr, 4).ToArray());
                if (len > 0) // PayLoad is allowed to be empty
                {
                    msgPtr += sizeof(Int32);
                    m.PayLoad = System.Text.Encoding.ASCII.GetString(source.Slice(msgPtr, len).ToArray());
                }
            }
            return m;
        }
    }
}
