using System;
using System.Collections.Generic;

namespace AARC.Model.Interfaces.RDS
{
    public interface IAarcRepository
    {
        Stock GetStock(string ticker);
        UnderlyingPrice GetUnderlyingPrice(string ticker, DateTime date);
        List<UnderlyingPrice> GetUnderlyingPrices(string ticker, DateTime startDate, DateTime endDate);
        List<UnderlyingPrice> GetUnderlyingPrices(string ticker);

        List<Option> GetOptions(string ticker, DateTime date);

        List<Tuple<Stock, double>> GetMomentumRanks(int momentumStrategyId, DateTime date, List<Stock> stocks, int numberToGet);
    }
}