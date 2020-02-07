using System;
using AARC.Mesh.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AARC.MeshTests
{
    [TestClass]
    public class DiscoveryMessageSerialization
    {
            private DiscoveryMessage _refMessage;
            private byte[] _rawBytes;
            private byte[] _inputBytes;

            [TestInitialize]
            public void SetUp()
            {
                _refMessage = new DiscoveryMessage
                {
                    HostServer = "localhost",
                    Port = 7777,
                    Payload = "test",
                     State = DiscoveryMessage.DiscoveryStates.Connect                    
                };
                _rawBytes = _refMessage.Encode(0);
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
                    var mm = new DiscoveryMessage();
                    mm.Decode(bytes);
                    Assert.AreEqual<string>(_refMessage.HostServer, mm.HostServer);
                    Assert.AreEqual<int>(_refMessage.Port, mm.Port);
                    Assert.AreEqual<string>(_refMessage.Payload, mm.Payload);
                    Assert.AreEqual<DiscoveryMessage.DiscoveryStates>(_refMessage.State, mm.State);
                };

                pp.DataReceived(_inputBytes, _inputBytes.Length);
            }
        }
    }
}
