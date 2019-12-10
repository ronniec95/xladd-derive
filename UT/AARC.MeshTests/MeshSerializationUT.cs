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
        [TestMethod]
        public void TestRegisterFileExists()
        {
            byte[] fileBytes = File.ReadAllBytes("Register.bin");

            Assert.IsNotNull(fileBytes);
        }

        [TestMethod]
        public void TestRegisterFileIsJson()
        {
            byte[] fileBytes = File.ReadAllBytes("Register.bin");

            Assert.IsNotNull(fileBytes);

            // We've gotten the length buffer
            int length = BitConverter.ToInt32(fileBytes, 0);

            Assert.AreEqual<int>(length, Math.Max(fileBytes.Length - 4, 0));
        }

        [TestMethod]
        public void TestRegisterFileToString()
        {
            byte[] fileBytes = File.ReadAllBytes("Register.bin");

            Assert.IsNotNull(fileBytes);

            // We've gotten the length buffer
            int length = BitConverter.ToInt32(fileBytes, 0);

            Assert.AreEqual<int>(length, Math.Max(fileBytes.Length - 4, 0));

            var json = System.Text.Encoding.ASCII.GetString(fileBytes, 4, length);

            Assert.IsNotNull(json);

            Assert.AreNotEqual<string>(json, string.Empty);
        }

        [TestMethod]
        public void TestRegisterFileToMeshMessage()
        {
            byte[] fileBytes = File.ReadAllBytes("Register.bin");

            Assert.IsNotNull(fileBytes);

            // We've gotten the length buffer
            int length = BitConverter.ToInt32(fileBytes, 0);

            Assert.AreEqual<int>(length, Math.Max(fileBytes.Length - 4, 0));

            var json = MeshUtilities.CloneReduce(fileBytes, 4, length);


            var meshmessage = MeshMessage.Deserialise(json);

            Assert.IsNotNull(meshmessage);

            Assert.IsNotNull(meshmessage.Channel);

            Assert.IsNotNull(meshmessage.PayLoad);
        }

        [TestMethod]
        public void TestGetInputQsFileExists()
        {
            byte[] fileBytes = File.ReadAllBytes("getinputqs.bin");

            Assert.IsNotNull(fileBytes);
        }

        [TestMethod]
        public void TestRGetInputQsFileIsJson()
        {
            byte[] fileBytes = File.ReadAllBytes("getinputqs.bin");

            Assert.IsNotNull(fileBytes);

            // We've gotten the length buffer
            int length = BitConverter.ToInt32(fileBytes, 0);

            Assert.AreEqual<int>(length, Math.Max(fileBytes.Length - 4, 0));
        }

        [TestMethod]
        public void TestGetInputQsFileToString()
        {
            byte[] fileBytes = File.ReadAllBytes("getinputqs.bin");

            Assert.IsNotNull(fileBytes);

            // We've gotten the length buffer
            int length = BitConverter.ToInt32(fileBytes, 0);

            Assert.AreEqual<int>(length, Math.Max(fileBytes.Length - 4, 0));

            var json = System.Text.Encoding.ASCII.GetString(fileBytes, 4, length);

            Assert.IsNotNull(json);

            Assert.AreNotEqual<string>(json, string.Empty);
        }

        [TestMethod]
        public void TestGetInputQsFileToMeshMessage()
        {
            byte[] fileBytes = File.ReadAllBytes("getinputqs.bin");

            Assert.IsNotNull(fileBytes);

            // We've gotten the length buffer
            int length = BitConverter.ToInt32(fileBytes, 0);

            Assert.AreEqual<int>(length, Math.Max(fileBytes.Length - 4, 0));

            var bytes = MeshUtilities.CloneReduce(fileBytes, 4, length);

            var meshmessage = MeshMessage.Deserialise(bytes);

            Assert.IsNotNull(meshmessage);

            Assert.IsNotNull(meshmessage.Channel);

            Assert.IsNotNull(meshmessage.PayLoad);
        }
    }
}
