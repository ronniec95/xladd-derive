using System;
using System.Collections.Generic;

namespace AARC.Model.Comparers
{
    public class TickerDateComparer : IEqualityComparer<AARC.Model.UnderlyingPrice>
    {
        public bool Equals(AARC.Model.UnderlyingPrice x, AARC.Model.UnderlyingPrice y)
        {
            return x.Ticker == y.Ticker && x.Date == y.Date;
        }

        public int GetHashCode(AARC.Model.UnderlyingPrice obj)
        {
            //Check whether the object is null
            if (Object.ReferenceEquals(obj, null)) return 0;

            //Get hash code for the Name field if it is not null.
            int hashTicker = obj.Ticker == null ? 0 : obj.Ticker.GetHashCode();
            //Get hash code for the Code field.
            int hashDate = obj.Date.GetHashCode();
            //Calculate the hash code for the obj.
            return hashTicker ^ hashDate;
        }
    }
}

