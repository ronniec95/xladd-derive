using System;
using System.Collections.Generic;

namespace AARC.Model
{
    public class YahooStockInfo
    {
        // candidates.Add(new { Ticker = yer.Symbol, yer.Time, optionsEtl.Quote.ShortName, optionsEtl.Quote.FullExchangeName, MarketCap = Math.Round(optionsEtl.Quote.MarketCap / 1e6, 2), BidAskSpread = Math.Round(100 * averageBidAskSpreadPercent, 2), Volume = volumeTraded, OpenInterest = openInterest });

        // Relatively stable stock information

        public string Ticker { get; set; }
        public string Company { get; set; }
        public string ShortName { get; set; }
        public string Exchange { get; set; }
        public string Sector { get; set; }
        public string Industry { get; set; }

        public double CurrentMarketCap { get; set; }
        public DateTime CurrentDate { get; set; }

        public DateTime NextEarningsDate { get; set; }
        public string Time { get; set; }
        public bool AfterClose { get; set; }

        public List<DateTime> Expirations { get; set; }
        public bool HasOptions { get; set; }
        public bool HasWeeklyOptions { get; set; }

        // variable, but has some longer term stability - used for candidate decision making
        public double BidAskSpread { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }

        public long SharesFloat { get; set; }
        public bool HasDividends { get; set; }
    }
}