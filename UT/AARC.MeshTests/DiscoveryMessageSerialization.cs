using System;
using System.Collections.Generic;
using System.Linq;
using AARC.Mesh;
using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using AARC.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static AARC.Mesh.Model.DiscoveryMessage;

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
                Service = new Uri("tcp://localhost:7777"),
                State = DiscoveryMessage.DiscoveryStates.Connect
            };
            _rawBytes = _refMessage.Encode(0);
            _inputBytes = PacketProtocol.WrapMessage(_rawBytes);
        }
        [TestMethod]
        public void TestRadixByteSerializationSimple1()
        {
            var s = "t";
            var r = Radix.Encode(s);
            var ds = Radix.Decode(r);
            Assert.AreEqual<string>(s.ToLower(), ds);

            var bytes = new List<byte>();
            Radix.RadixToBytes(bytes, r);
            var msgPtr = 0;
            var ds2 = Radix.BytesToRadix(bytes.ToArray(), ref msgPtr);
            CollectionAssert.AreEqual(r, ds2);

            Assert.AreEqual<int>(9, msgPtr);

        }
        [TestMethod]
        public void TestRadixByteSerializationSimple2()
        {
            var s = "abcdefghijkl";
            var r = Radix.Encode(s);
            var ds = Radix.Decode(r);
            Assert.AreEqual<string>(s.ToLower(), ds);

            var bytes = new List<byte>();
            Radix.RadixToBytes(bytes, r);
            var msgPtr = 0;
            var ds2 = Radix.BytesToRadix(bytes.ToArray(), ref msgPtr);
            CollectionAssert.AreEqual(r, ds2);

            Assert.AreEqual<int>(9, msgPtr);

        }
        [TestMethod]
        public void TestBytes()
        {
            var r = new List<UInt64>( new UInt64 [] { 22 });
            var bytes = new List<byte>();
            Radix.RadixToBytes(bytes, r);
            var msgPtr = 0;
            var r2 = Radix.BytesToRadix(bytes.ToArray(), ref msgPtr);
            CollectionAssert.AreEqual(r, r2);
        }
        [TestMethod]
        public void TestRadixByteSerializationSimple3()
        {
            var s = "abcdefghijklm";
            var r = Radix.Encode(s);
            var ds = Radix.Decode(r);
            Assert.AreEqual<string>(s.ToLower(), ds);

            var bytes = new List<byte>();
            Radix.RadixToBytes(bytes, r);
            var msgPtr = 0;
            var ds2 = Radix.BytesToRadix(bytes.ToArray(), ref msgPtr);
            CollectionAssert.AreEqual(r, ds2);

            Assert.AreEqual<int>(17, msgPtr);

        }
        [TestMethod]
        public void TestRadixByteSerializationWithAddress()
        {
            var s = "tcp://localhost:7777";
            var r = Radix.Encode(s);
            var ds = Radix.Decode(r);
            Assert.AreEqual<string>(s.ToLower(), ds);

            var bytes = new List<byte>();
            Radix.RadixToBytes(bytes, r);
            var msgPtr = 0;
            var ds2 = Radix.BytesToRadix(bytes.ToArray(), ref msgPtr);
            CollectionAssert.AreEqual(r, ds2);

            Assert.AreEqual<int>(17, msgPtr);

        }

        [TestMethod]
        public void TestRadixByteSerializationWithChannels()
        {
            var dm = new DiscoveryMessage { Service = _refMessage.Service, State = _refMessage.State };
            dm.Channels = new List<MeshChannel>(
                new[] {
                   
                new MeshChannel
                {
                    Name = "testchannel",
                    Addresses = new HashSet<Uri> { new Uri("tcp://test:01"), new Uri("tcp://test:02") },
                    ChannelType = MeshChannel.ChannelTypes.Input,
                    EncodingType = 1,
                    PayloadType = "test",
                    Instance = 2
                }
                }
                );

            var bytes = dm.Encode(0);
            var ddm = new DiscoveryMessage();
            ddm.Decode(bytes);
            Assert.AreEqual<Uri>(dm.Service, ddm.Service);
            Assert.AreEqual<DiscoveryMessage.DiscoveryStates>(dm.State, ddm.State);

            Assert.AreEqual<int>(dm.Channels.Count, ddm.Channels.Count);

            for (var i = 0; i < dm.Channels.Count; i++)
            {
                Assert.AreEqual<MeshChannel.ChannelTypes>(dm.Channels[i].ChannelType, ddm.Channels[i].ChannelType);
                Assert.AreEqual<UInt64>(dm.Channels[i].Instance, ddm.Channels[i].Instance);
                Assert.AreEqual<byte>(dm.Channels[i].EncodingType, ddm.Channels[i].EncodingType);
                Assert.AreEqual<string>(dm.Channels[i].PayloadType, ddm.Channels[i].PayloadType);
                CollectionAssert.AreEqual(dm.Channels[i].Addresses.ToList(), ddm.Channels[i].Addresses.ToList());
            }
        }

        [TestMethod]
        public void TestEncodedMessageExits() => Assert.IsNotNull(_inputBytes);

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
            pp.MessageArrived += (bytes) =>
            {
                var mm = new DiscoveryMessage();
                mm.Decode(bytes);
                Assert.AreEqual<Uri>(_refMessage.Service, mm.Service);
                CollectionAssert.AreEquivalent(_refMessage.Channels, mm.Channels);
                Assert.AreEqual<DiscoveryMessage.DiscoveryStates>(_refMessage.State, mm.State);
            };

            pp.DataReceived(_inputBytes, _inputBytes.Length);
        }

        [TestMethod]
        public void TestDiscoveryStateInitialState()
        {
            var dsm = new DiscoveryServiceStateMachine<DiscoveryMessage>();

            var hostname = new Uri("tcp://testhostname:9999");

            var dm = new DiscoveryMessage();
            dsm.CreateSendMessage(dm, hostname);
            Assert.AreEqual<DiscoveryStates>(DiscoveryStates.Connect, dm.State);
        }

        [TestMethod]
        public void TestDiscoveryStateChannelDataState()
        {
            var dsm = new DiscoveryServiceStateMachine<DiscoveryMessage>();

            var hostname = new Uri("tcp://testhostname:9999");

            var dm = new DiscoveryMessage { Service = hostname, State = DiscoveryStates.ConnectResponse };
            dsm.CreateReceiveMessage(dm);
            dsm.CreateSendMessage(dm, hostname);
            Assert.AreEqual<DiscoveryStates>(DiscoveryStates.ChannelData, dm.State);
        }

        [TestMethod]
        public void TestDiscoveryStateChannelDataStateWithChannel()
        {
            var dsm = new DiscoveryServiceStateMachine<MeshMessage>();

            var transportId = new Uri($"tcp://testhostname:9999");

            var dm = new DiscoveryMessage { Service = transportId, State = DiscoveryStates.ConnectResponse };
            dsm.CreateReceiveMessage(dm);
            dsm.LocalInputChannels["testchannel"] = new MeshNetChannel<MeshMessage>();
            dsm.CreateSendMessage(dm, transportId);
            Assert.AreEqual<DiscoveryStates>(DiscoveryStates.ChannelData, dm.State);
            Assert.AreEqual<int>(1, dm.Channels.Count);
            Assert.AreEqual<string>("testchannel", dm.Channels.FirstOrDefault().Name);
            CollectionAssert.AreEqual(new Uri[] { transportId }, dm.Channels.FirstOrDefault().Addresses.ToList());
        }


        [TestMethod]
        public void TestDiscoveryStateChannelDataStateWithChannelEncode()
        {
            var dsm = new DiscoveryServiceStateMachine<MeshMessage>();

            var transportId = new Uri("tcp://testhostname:9999");

            var dm = new DiscoveryMessage { Service = transportId, State = DiscoveryStates.ConnectResponse };
            dsm.CreateReceiveMessage(dm);
            dsm.LocalInputChannels["testchannel"] = new MeshNetChannel<MeshMessage>();
            dsm.CreateSendMessage(dm, transportId);
            Assert.AreEqual<DiscoveryStates>(DiscoveryStates.ChannelData, dm.State);
            Assert.AreEqual<int>(1, dm.Channels.Count);
            Assert.AreEqual<string>("testchannel", dm.Channels.FirstOrDefault().Name);

            var bytes = dm.Encode(0);
            var ddm = new DiscoveryMessage();
            ddm.Decode(bytes);
            var bytes2 = ddm.Encode(0);
            Assert.AreEqual<int>(bytes.Length, bytes2.Length);
        }

        [TestMethod]
        public void TestNTP()
        {
            var bytes = new List<byte>();
            var (totalSeconds, milliseconds) = DateTimeUtilities.DateTimeToUnixTotalSeconds(new DateTime(1980, 1, 1));
            bytes.AddRange(BitConverter.GetBytes(totalSeconds));
            bytes.AddRange(BitConverter.GetBytes(milliseconds));

            var bytearray = bytes.ToArray();

            MeshUtilities.UpdateNT(new DateTime(1990, 1, 1), bytearray);

            var msgPtr = 0;
            var newtotalseconds = bytearray.ToUInt64(ref msgPtr);
            Assert.AreNotEqual<UInt64>(totalSeconds, newtotalseconds);
            var newms = bytearray.ToUInt32(ref msgPtr);
            Assert.AreEqual<UInt32>(milliseconds, newms);

        }
    }
}
