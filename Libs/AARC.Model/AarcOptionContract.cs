using System;

namespace AARC.Model
{
    public class AarcOptionContract : AarcOptionBase
    {
        public string Ticker { get; set; }

        public DateTime Date { get; set; }
        public DateTime Expiry { get; set; }
        public double Strike { get; set; }
        public string PutCall { get; set; }
        public string Exchange { get; set; }
        public string Currency { get; set; }

        public string Symbol => Ticker;
        public string LocalSymbol => $"{Ticker}{Expiry:yyyyMMdd}{PutCall:1}{Strike}";
    }
}
