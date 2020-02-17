using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AARC.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AARC.MeshTests
{
    [TestClass]
    public class RedixUT
    {
        [TestMethod]
        public void TestEncodeString()
        {
            Assert.AreEqual<char>('\r', Radix.radixconst[0]);
            Assert.AreEqual<char>('a', Radix.radixconst[1]);
        }
        [TestMethod]
		public void RedixTest()
		{
            // Todo: What about type info in channels?
            var x = new string[] { "test1","TEST2", ".", "dumb.method", "dumb.method.", "dumb.method.i", "dumb.method.in", "Dumb.Method1.Input1", "Dumb.Return" };
			foreach(var s in x)
            {
                var r = Radix.Encode(s);
                var ds = Radix.Decode(r);
                Assert.AreEqual<string>(s.ToLower(), ds);
            }

		}

        [TestMethod]
        public void RadixTestTcpServiceAddress()
        {
            var s = "tcp://localhost:7777";
            var r = Radix.Encode(s);
            var ds = Radix.Decode(r);
            Assert.AreEqual<string>(s.ToLower(), ds);
        }

        [TestMethod]
        public void RadixTestSimpleServiceAddress()
        {
            var s = "abMac.local:0";
            var r = Radix.Encode(s);
            var ds = Radix.Decode(r);
            Assert.AreEqual<string>(s.ToLower(), ds);
        }

        [TestMethod]
        public void Test3()
        {
            string base2 = "111";
            string base8 = "117";
            string base10 = "1110";
            string base16 = "11F1FF";

            var b2 = Radix.Encode(base2);


            var b8 = Radix.Encode(base8);


            var b10 = Radix.Encode(base10);


            var b16 = Radix.Encode(base16);

            Assert.AreEqual<string>(base2, Radix.Decode(b2));
            Assert.AreEqual<string>(base8, Radix.Decode(b8));
            Assert.AreEqual<string>(base10, Radix.Decode(b10));
            Assert.AreNotEqual<string>(base16, Radix.Decode(b16));
            Assert.AreEqual<string>(base16.ToLower(), Radix.Decode(b16));
        }

	}
 }
