using AARC.Model.Interfaces;
using System.Collections.Generic;

namespace AARC.Model
{
    public class TickerPrices : IAarcPrice
    {
        public string Ticker { get; set; }
        public IList<uint> Dates { get; set; }
        public IList<double> Open { get; set; }
        public IList<double> High { get; set; }
        public IList<double> Low { get; set; }
        public IList<double> ClosingPrices { get; set; }
        public IList<double> Volume { get; set; }
    }
}
