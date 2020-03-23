using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

namespace AARC.Model
{
    public static class UnderlyingPriceExtensions
    {
        public static UnderlyingPrice Clone(this UnderlyingPrice p)
        {
            UnderlyingPrice c = new UnderlyingPrice();
            c.idHistoricalData = p.idHistoricalData;
            c.Ticker = p.Ticker;
            c.CreatedDate = p.CreatedDate;
            c.Date = p.Date;
            c.Open = p.Open;
            c.Close = p.Close;
            c.High = p.High;
            c.Low = p.Low;
            c.Volume = p.Volume;
            c.Intraday = p.Intraday;
            c.AdjustUpdate = p.AdjustUpdate;
            c.AdjustedClose = p.AdjustedClose;
            return c;
        }

        public static void ValueCopy(this UnderlyingPrice c, UnderlyingPrice p)
        {
            if (!DoubleEquals(c.Open, p.Open))
                c.Open = p.Open;
            if (!DoubleEquals(c.Close, p.Close))
                c.Close = p.Close;
            if (!DoubleEquals(c.High, p.High))
                c.High = p.High;
            if (!DoubleEquals(c.Low, p.Low))
                c.Low = p.Low;
            if (!LongEquals(c.Volume, p.Volume))
                c.Volume = p.Volume;
            if (c.Intraday != p.Intraday)
                c.Intraday = p.Intraday;
            if (!DoubleEquals(c.AdjustedClose, p.AdjustedClose))
                c.AdjustedClose = p.AdjustedClose;
        }

        public static bool DoubleEquals(this double? x, double y)
        {
            if (!x.HasValue)
                return false;

            return Math.Abs(x.Value - y) < Double.Epsilon;
        }

        public static bool DoubleEquals(this double? x, double? y)
        {
            if (x.HasValue != y.HasValue)
                return false;

            return Math.Abs(x.Value - y.Value) < Double.Epsilon;
        }
        public static bool LongEquals(this long? x, long? y)
        {
            if (x.HasValue != y.HasValue)
                return false;

            return x == y;
        }

        public static bool ValueEquals(this UnderlyingPrice x, UnderlyingPrice y)
        {
            return
                Math.Abs(x.Close - y.Close) < Double.Epsilon
                && x.Volume.LongEquals(y.Volume)
                && x.Open.DoubleEquals(y.Open)
                && x.High.DoubleEquals(y.High)
                && x.Low.DoubleEquals(y.Low)
                && x.AdjustedClose.DoubleEquals(y.AdjustedClose);
        }
    }

    [Table("UnderlyingPrices")]
    public class UnderlyingPrice : Interfaces.IAarcTicker
    {
        [Key]
        public int idHistoricalData { get; set; }

        public string Ticker { get; set; }
        public DateTime CreatedDate { get; set; }
        [JsonProperty("index")]
        public DateTime Date { get; set; }
        public double? Open { get; set; }
        public double Close { get; set; }

        public double? High { get; set; }
        public double? Low { get; set; }
        public long? Volume { get; set; }
        public bool Intraday { get; set; }

        public DateTime? AdjustUpdate { get; set; }
        public double? AdjustedClose { get; set; }

        public UnderlyingPrice() { }

        public UnderlyingPrice(string ticker)
        {
            Ticker = ticker;
        }
    }
}
