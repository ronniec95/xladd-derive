using System;
using System.Linq;
using System.Text;
using AARC.Mesh.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AARC.MeshTests
{
    [TestClass]
    public class PacketProtocolUT
    {
        [TestMethod]
        public void TestMethod1()
        {
            int messages = 0;
            var packetizer = new PacketProtocol(2000);
            packetizer.MessageArrived += _ => ++messages;
            var message = PacketProtocol.WrapMessage(new byte[3]).Concat(
            PacketProtocol.WrapMessage(new byte[4])).ToArray();
            packetizer.DataReceived(message, message.Length);
            Assert.AreEqual(2, messages);
        }

        [TestMethod]
        public void TestMethod2()
        {
            int numMessages = 0;
            var messages = new string[2];
            var packetizer = new PacketProtocol(2000);
            packetizer.MessageArrived += message =>
            {
                messages[numMessages] = Encoding.UTF8.GetString(message);
                ++numMessages;
            };
            var rawmessage = PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("Hello")).Concat(
            PacketProtocol.WrapMessage(Encoding.UTF8.GetBytes("World"))).ToArray();
            packetizer.DataReceived(rawmessage, rawmessage.Length);

            Assert.AreEqual(2, numMessages);
            Assert.AreEqual("Hello", messages[0]);
            Assert.AreEqual("World", messages[1]);
        }

        [TestMethod]
        public void TestMethod3()
        {
            string[] messages = { @"{ Action:""newprice"", Message:""BUM"", Service: ""calcprocessor"" }", @"{ Action:""newti"", Message:""0"", Service: ""calcprocessor"" }" };
            var packetizer = new PacketProtocol(2000);
            int count = 0;
            packetizer.MessageArrived += message =>
            {
                var s = Encoding.UTF8.GetString(message);
                Console.WriteLine($"[{++count}]New Message ({message.Length}) [{s}]");
            };
            for (int i = 0; i < 1000; i++)
                foreach (var message in messages)
                {
                    var bytes = Encoding.UTF8.GetBytes(message);
                    var formatedBytes = PacketProtocol.WrapMessage(bytes);
                    packetizer.DataReceived(formatedBytes, formatedBytes.Length);
                }
        }
    }
}
