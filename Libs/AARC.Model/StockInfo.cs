using System;
using System.Collections.Generic;
using System.Linq;

namespace AARC.Model
{
    public class StockInfo
    {
        public StockInfo()
        {
        }

        public int Index { get; set; }
        public string Attribute { get; set; }
        public string Value { get; set; }

        public static Stock ToStock(IEnumerable<StockInfo> objects)
        {
            return new Stock
            {
                MarketCap = ToDouble(objects.Where(o => o.Attribute.StartsWith("Market Cap")).Select(m => m.Value).FirstOrDefault()),
            };
        }

        public static double ToDouble(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0.0;
            if (value.EndsWith("B"))
                return Convert.ToDouble(value.Substring(0, value.Length - 1)) * 100000000;
            if (value.EndsWith("M"))
                return Convert.ToDouble(value.Substring(0, value.Length - 1)) * 1000000;
            if (value.EndsWith("K"))
                return Convert.ToDouble(value.Substring(0, value.Length - 1)) * 1000;
            return Convert.ToDouble(value);
        }
    }
}
