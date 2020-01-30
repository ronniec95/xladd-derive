using AARC.Model;

namespace AARC.Repository.Interfaces
{
    public interface IStockSplitRepository
    {
        StockSplit GetStockSplit(string symbol);
        void AddOrUpdate(StockSplit stockSplit);
    }
}