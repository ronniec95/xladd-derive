using AARC.Model;
using AARC.Model.Interfaces;
using AARC.RDS;
using AARC.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AARC.Repository.EF
{
    public class StockRepository : IStockRepository
    {
        protected IStocksDataContext _context;

        public StockRepository(AARCContext context)
        {
            _context = context;
        }

        public void Delete(string symbol)
        {
            var stock = _context.Stocks.Where(s => s.Ticker == symbol).FirstOrDefault();
            _context.Stocks.Remove(stock);
            _context.SaveChanges();
        }

        public IEnumerable<Stock> GetAllStocks() => _context.Stocks.AsNoTracking();

        public Stock GetStock(string symbol) => _context.Stocks.AsNoTracking().Where(s => s.Ticker == symbol).FirstOrDefault();

        public void Overwrite(Stock stock)
        {
            throw new NotImplementedException();
        }

        public void Overwrite(IList<Stock> stocks)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Stock> GetStocksByExchange(string exchange) => _context.Stocks.AsNoTracking().Where(s => s.Exchange == exchange);

        public IEnumerable<Stock> GetStocksBySector(string sector) => _context.Stocks.AsNoTracking().Where(s => s.Sector == sector);

        public IEnumerable<Stock> GetStocksByGreaterEqualMarketCap(long cap) => _context.Stocks.AsNoTracking().Where(s => s.MarketCap >= cap);
    }
}
