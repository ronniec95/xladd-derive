using System.Collections.Generic;

namespace aarcYahooFinETL.Controllers
{
    public static class StockExtensions
    {
        public static void ToStock(this AARC.Model.Stock stock, Dictionary<string, string> dic)
        {
            foreach (var kv in dic)
            {
                switch (kv.Key)
                {
                    case @"Market Cap":
                        stock.MarketCap = AARC.Model.StockInfo.ToDouble(kv.Value);
                        break;
                    case @"Forward Dividend & Yield":
                    case @"Earnings Date":
                    case @"Beta (3Y Monthly)":
                    case @"PE Ratio (TTM)":
                    case @"EPS (TTM)":
                    case @"Ex-Dividend Date":
                    default:
                        break;
                }
            }
        }

        public static void ToStock(this AARC.Model.Stock stock, IEnumerable<AARC.Model.StockInfo> stockInfos)
        {
            foreach (var stockInfo in stockInfos)
            {
                switch (stockInfo.Attribute)
                {
                    case @"Market Cap":
                        stock.MarketCap = AARC.Model.StockInfo.ToDouble(stockInfo.Value);
                        break;
                    case @"Forward Dividend & Yield":
                    case @"Earnings Date":
                    case @"Beta (3Y Monthly)":
                    case @"PE Ratio (TTM)":
                    case @"EPS (TTM)":
                    case @"Ex-Dividend Date":
                    default:
                        break;
                }
            }
        }
    }
}
