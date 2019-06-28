using System;
using System.Collections.Generic;

namespace AARC.Model.Comparers
{
    public class ClosePriceComparer : IEqualityComparer<UnderlyingPrice>
    {
        public static bool DoubleEquals(double? x, double? y)
        {
            if (x.HasValue != y.HasValue)
                return false;

            return Math.Abs(x.Value - y.Value) < Double.Epsilon;
        }

        public bool Equals(UnderlyingPrice x, UnderlyingPrice y)
        {
            return x.Ticker == y.Ticker
                && x.Date == y.Date
                && Math.Abs(x.Close - y.Close) < Double.Epsilon
                && DoubleEquals(x.High, y.High)
                && DoubleEquals(x.Low, y.Low)
                && DoubleEquals(x.AdjustedClose, y.AdjustedClose);
        }

        public int GetHashCode(UnderlyingPrice obj)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(obj, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashTicker = obj.Ticker == null ? 0 : obj.Ticker.GetHashCode();

            //Get hash code for the Code field.
            int hashDate = obj.Date.GetHashCode();

            //Get hash code for the Code field.
            int hashClose = obj.Close.GetHashCode();
            int hashHigh = obj.High.GetHashCode();
            int hashLow = obj.Low.GetHashCode();

            int hashAdjusted = obj.AdjustedClose.GetHashCode();
            //Calculate the hash code for the obj.
            return hashTicker ^ hashDate ^ hashClose ^ hashHigh ^ hashLow ^ hashAdjusted;
        }
    }
}
