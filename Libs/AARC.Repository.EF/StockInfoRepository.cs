using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AARC.Model.Interfaces.RDS;
using AARC.Model;
using AARC.RDS;

namespace AARC.Repository.EF
{
    public class StockInfoRepository : IRepository<StockInfo>
    {
        IStocksDataContext _context;
        ILogger<StockInfo> _logger;

        public StockInfoRepository(AARCContext context, ILogger<StockInfo> logger)
        {
            _context = (IStocksDataContext)context;
            _logger = logger;
        }

        public IEnumerable<StockInfo> Get()
        {
            throw new NotImplementedException();
        }

        public DateTime? GetMaxDate(string ticker)
        {
            throw new NotImplementedException();
        }

        public UpsertStat Upsert(StockInfo Object)
        {
            throw new NotImplementedException();
        }

        public UpsertStat Upsert(IEnumerable<StockInfo> Objects)
        {
            throw new NotImplementedException();
            var latestStocks = StockInfo.ToStock(Objects);

            var dbRows = _context.Stocks
                .ToList();


            var changeStats = new UpsertStat();
            return changeStats;
        }

        UpsertStat IRepository<StockInfo>.Upsert(IEnumerable<StockInfo> Objects)
        {
            throw new NotImplementedException();
        }
    }
}
