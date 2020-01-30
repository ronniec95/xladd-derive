using System;

namespace AARC.Repository.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IStockRepository Stocks { get; }
        IIndexStockRepository IndexStocks { get; }
        IUnderlyingRepository UnderlyingPrices { get; }
        IStockSplitRepository StockSplits { get; }
        IDividendRepository Dividends { get; }
        IOptionsRepository Options { get; }
        IEarningsRepository Earnings { get; }
        IEarningsSurprizeRepository EarningsSurprizes { get; }
        IEarningConsensusRepository EarningConsensui { get; }
        IEarningsDilutionsRepository EarningsDilutions { get; }
        int Complete();
    }
}
