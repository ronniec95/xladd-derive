using System;
using System.Buffers;
using System.IO;
using AARC.Mesh;
using AARC.Mesh.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AARC.MeshTests
{
    [TestClass]
    public class MeshSerializationUT
    {
        private MeshMessage _refMessage;
        private byte[] _rawBytes;
        private byte[] _inputBytes;

        [TestInitialize]
        public void SetUp()
        {
            _refMessage = new MeshMessage
            {
                GraphId = 0,
                XId = MeshUtilities.NewXId,
                Service = "tcp:/localhost:1000",
                Channel = "TestChannel",
                PayLoad = null
            };
            _rawBytes = _refMessage.Encode();
            _inputBytes = PacketProtocol.WrapMessage(_rawBytes);
        }

        [TestMethod]
        public void TestEncodedMessageExits()
        {
            Assert.IsNotNull(_inputBytes);
        }

        [TestMethod]
        public void TestRegisterEncodedMessageHasSize()
        {
            Assert.IsNotNull(_inputBytes);

            // We've gotten the length buffer
            int length = BitConverter.ToInt32(_inputBytes, 0);

            Assert.AreEqual<int>(length, Math.Max(_inputBytes.Length - 4, _rawBytes.Length));
        }

        [TestMethod]
        public void TestRegisterEncodeMessageDecode()
        {
            Assert.IsNotNull(_inputBytes);

            var pp = new PacketProtocol(Math.Max(1024, _inputBytes.Length));
            pp.MessageArrived += (bytes) => {
                var mm = new MeshMessage();
                mm.Decode(bytes);
                Assert.AreEqual<uint>(_refMessage.GraphId, mm.GraphId);
                Assert.AreEqual<uint>(_refMessage.XId, mm.XId);
                Assert.AreEqual<string>(_refMessage.Service, mm.Service);
                Assert.AreEqual<string>(_refMessage.Channel, mm.Channel);
                Assert.AreEqual<string>(_refMessage.PayLoad, mm.PayLoad);
            };

            pp.DataReceived(_inputBytes, _inputBytes.Length);
        }
    }
}
