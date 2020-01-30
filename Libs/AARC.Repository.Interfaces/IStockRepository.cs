using System.Collections.Generic;
using AARC.Model;

namespace AARC.Repository.Interfaces
{
   public interface IStockRepository
    {
        Stock GetStock(string symbol);
        IEnumerable<Stock> GetAllStocks();

        void Overwrite(Stock stock);
        void Overwrite(IList<Stock> stocks);
        void Delete(string symbol);
        IEnumerable<Stock> GetStocksByExchange(string exchange);
        IEnumerable<Stock> GetStocksBySector(string sector);
        IEnumerable<Stock> GetStocksByGreaterEqualMarketCap(long cap);
        IEnumerable<Stock> GetStocksByWithOptionsMarketCap();
    }
}
