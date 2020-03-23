using System;
using System.Collections.Generic;
using AARC.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AARC.MeshTests
{
    [TestClass]
    public class yahooUnderlyingPricesUT
    {
        [TestMethod]
        public void loadAppleJsonNullpriceTest()
        {
            try
            {
                var data = System.IO.File.ReadAllText("TestData/AAPL_20200319135814.json");
                Assert.IsNotNull(data);
                var entities = JsonConvert.DeserializeObject<List<UnderlyingPrice>>(data, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                CollectionAssert.AllItemsAreNotNull(entities);
            }
            catch(Exception ex)
            {
                Assert.IsNotNull(ex);
            }
        }
    }
}
