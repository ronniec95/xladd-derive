using System;
using System.Collections.Generic;
using AARC.Model;

namespace AARC.Model.Comparers
{
    public class TickerComparer : IEqualityComparer<Stock>
    {
        public bool Equals(Stock x, Stock y)
        {
            return x.Ticker == y.Ticker;
        }

        public int GetHashCode(Stock obj)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(obj, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashTicker = obj.Ticker == null ? 0 : obj.Ticker.GetHashCode();

            //Calculate the hash code for the obj.
            return hashTicker;
        }
    }
}
