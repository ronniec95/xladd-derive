using aarcYahooFinETL.DataSource;
using NUnit.Framework;

namespace aarcELT.UT
{
    public class ClosePriceRepositotyUT
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
    }
}
