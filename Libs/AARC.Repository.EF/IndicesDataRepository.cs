using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AARC.Model.Interfaces.RDS;
using System.Threading.Tasks;
using AARC.RDS;
using AARC.Model;

namespace AARC.Repository.EF
{
    public partial class IndicesDataRepository : IRepository<Stock>
    {
        IStocksDataContext _context;
        ILogger<Stock> _logger;
        IEqualityComparer<Stock> _comparer;

        public IndicesDataRepository(AARCContext context, IEqualityComparer<Stock> comparer, ILogger<Stock> logger)
        {
            _context = (IStocksDataContext)context;
            _logger = logger;
            _comparer = comparer; // new Comparers.TickerComparer()
        }

        public IEnumerable<Stock> Get()
        {
            var objects = _context.Stocks.ToList();

            return objects;
        }

        public DateTime? GetMaxDate(string ticker)
        {
            throw new NotImplementedException();
        }

        public UpsertStat Upsert(Stock Object)
        {
            throw new NotImplementedException();
        }

        public UpsertStat Upsert(IEnumerable<Stock> latestStocks)
        {
            var today = DateTime.Now.Date;
            var dbRows = _context.Stocks
                .ToList();

            var newEntities = latestStocks
                .Except(dbRows, _comparer)
                .ToList();


            var unchangedEntities = 0;
            var updatedEnities = 0;
            dbRows.ForEach(r =>
            {
                var q = latestStocks
                    .Where(u => u.Ticker == r.Ticker)
                    .Select(u => new { Match = r.MarketCap == u.MarketCap, Data = u })
                    .FirstOrDefault();
                if (q != null) 
                {
                    if (q.Match)
                        unchangedEntities++;
                    else
                    {
                        r.MarketCap = q.Data.MarketCap;
                        updatedEnities++;
                    }
                }
            }
            );

            Parallel.ForEach(newEntities, np => { np.LastUpdated = today; });
 //           newEntities = newEntities.Take(2).ToList();
            _context.AddRange(newEntities);
            _logger.LogInformation("Start Save");
            _context.SaveChanges();
            _logger.LogInformation($"End Save unchanged {unchangedEntities}, new {newEntities.Count}, updated {updatedEnities}");

            var changeStats = new UpsertStat { NewRows = newEntities.Count, UpdatedRows = updatedEnities, UnchangedRows = unchangedEntities, TotalRows = _context.Stocks.Where(s => latestStocks.Where(l => l.Ticker == s.Ticker).Any()).Count() };
            return changeStats;
        }
    }
}
