using System;
using System.Collections.Generic;

namespace AARC.Model.Comparers
{
    public class StockComparer : IEqualityComparer<AARC.Model.Stock>
    {
        public static bool DoubleEquals(double? x, double? y)
        {
            if (x.HasValue != y.HasValue)
                return false;

            return Math.Abs(x.Value - y.Value) < Double.Epsilon;
        }
        public static bool LongEquals(long? x, long? y)
        {
            if (x.HasValue != y.HasValue)
                return false;

            return x == y;
        }
        public bool Equals(AARC.Model.Stock x, AARC.Model.Stock y)
        {
            return x.Ticker == y.Ticker
                && DoubleEquals(x.MarketCap, y.MarketCap);
            //                && x.Industry == y.Industry
            //                && x.HasOptions == y.HasOptions;
        }

        public int GetHashCode(AARC.Model.Stock obj)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(obj, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashTicker = obj.Ticker == null ? 0 : obj.Ticker.GetHashCode();

            //Get hash code for the Code field.
            int hashDate = obj.MarketCap.GetHashCode();

            //Calculate the hash code for the obj.
            return hashTicker ^ hashDate;
        }
    }
}
