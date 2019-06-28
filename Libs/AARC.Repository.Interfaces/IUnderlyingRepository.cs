using System;
using System.Collections.Generic;
using AARC.Model;

namespace AARC.Repository.Interfaces
{
    public interface IUnderlyingRepository
    {
        IEnumerable<UnderlyingPrice> GetUnderlyingPrices(string symbol);

        UnderlyingPrice GetUnderlyingPrice(string symbol, DateTime date);

        UnderlyingPrice GetUnderlyingPrices(string symbol, DateTime startDate, DateTime endDate);

        void AddOrUpdate(UnderlyingPrice underlyingPrice);

        void Overwrite(UnderlyingPrice underlyingPrice);

        void Overwrite(IList<UnderlyingPrice> underlyingPrices);

        bool HasDate(string symbol, DateTime date);
    }
}