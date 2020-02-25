using System;
using System.Net;
using AARC.Mesh;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AARC.MeshTests
{
    [TestClass]
    public class NetworkUT
    {
        [TestMethod]
        public void CheckDnsHostName()
        {
            var hostname = Dns.GetHostName();
            Assert.IsNotNull(hostname);
            Assert.AreNotEqual<string>(string.Empty, hostname);
        }

        [TestMethod]
        public void CheckDnsHostName2()
        {
            var hostname = Dns.GetHostName();
            Assert.IsNotNull(hostname);
            Assert.AreNotEqual<string>(string.Empty, hostname);

            var hostname2 = MeshUtilities.GetLocalHostFQDN();

            Assert.AreEqual<string>(hostname, hostname2);
        }
    }
}
