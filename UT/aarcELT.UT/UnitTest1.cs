using System.IO;
using AARC.Utilities;
using aarcYahooFinETL.DataSource;
using NUnit.Framework;

namespace Tests
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            var client = new MockClosingPricesClient("/Volumes/Thunder1/Homes/ab/Projects/webscrapers/ClosePrices");
            var r = client.Request("AMZN").Result;
            Assert.Pass();
        }

        [Test]
        public void Test2()
        {
            var path = "abcdef";

            var r = PathHelper.GetHeadPath(path);

            Assert.AreEqual(path, r);
        }

        [Test]
        public void Test3()
        {
            var test = "abcdef";
            var path = Path.Combine("/", test);

            var r = PathHelper.GetHeadPath(path);

            Assert.AreEqual(test, r);
        }

        [Test]
        public void Test4()
        {
            var test = "abcdef";
            var path = $"/{test}/";

            var r = PathHelper.GetHeadPath(path);

            Assert.AreEqual(test, r);
        }

        [Test]
        public void Test5()
        {
            var test = "abcdef";
            var path = $"/{test}/foo/boo";

            var r = PathHelper.GetHeadPath(path);

            Assert.AreEqual(test, r);
        }

        [Test]
        public void Test6()
        {
            var path = "";

            var r = PathHelper.GetHeadPath(path);

            Assert.AreEqual(path, r);
        }

        [Test]
        public void Test7()
        {
            string path = null;

            var r = PathHelper.GetHeadPath(path);

            Assert.AreEqual(string.Empty, r);
        }
    }
}