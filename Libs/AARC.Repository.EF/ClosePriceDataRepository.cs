using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AARC.Model.Interfaces.RDS;
using System.Threading.Tasks;
using AARC.Model;
using AARC.RDS;
using Microsoft.EntityFrameworkCore;

namespace AARC.Repository.EF
{
    public class ClosePriceDataRepository : IRepository<UnderlyingPrice>
    {
        IClosePriceDataContext _context;
        ILogger<ClosePriceDataRepository> _logger;
        IEqualityComparer<AARC.Model.UnderlyingPrice> _comparer;

        public ClosePriceDataRepository(AARCContext context, IEqualityComparer<AARC.Model.UnderlyingPrice> comparer, ILogger<ClosePriceDataRepository> logger)
        {
            _context = (IClosePriceDataContext)context;
            _comparer = comparer; // new Comparers.TickerDateComparer()
            _logger = logger;
        }

        public IEnumerable<UnderlyingPrice> Get()
        {
            return _context.UnderlyingPrices;
        }

        public IEnumerable<UnderlyingPrice> Get(string ticker)
        {
            return _context.UnderlyingPrices
                .AsNoTracking()
                .Where(u => u.Ticker == ticker);
        }

        public IEnumerable<UnderlyingPrice> Get(string ticker, DateTime startDate, DateTime endDate)
        {
            return _context.UnderlyingPrices
                .AsNoTracking()
                .Where(u => u.Ticker == ticker)
                .Where(u => u.Date >= startDate && u.Date <= endDate);
        }

        public IEnumerable<double> GetClosePrices(string ticker, DateTime startDate, DateTime endDate)
        {
            return _context.UnderlyingPrices
                .AsNoTracking()
                .Where(u => u.Ticker == ticker)
                .Where(u => u.Date >= startDate && u.Date <= endDate)
                .Select(u => u.AdjustedClose ?? u.Close);
        }

        public DateTime? GetMaxDate(string ticker)
        {
            var elements = _context.UnderlyingPrices
                .AsNoTracking()
                   .Where(u => u.Ticker == ticker);

            if (elements.Any())
                return elements?.Max(e => e.Date);

            return null;

        }

        public UpsertStat Upsert(IEnumerable<UnderlyingPrice> Entities)
        {
            var today = DateTime.Now;
            var maxDate = Entities.Max(u => u.Date);
            var minDate = Entities.Min(u => u.Date);
            var tickers = Entities.Select(e => e.Ticker).Distinct();
            var ticker = Entities.Select(u => u.Ticker).FirstOrDefault();
            var dbRows = _context.UnderlyingPrices
                .Where(u => u.Ticker == ticker && u.Date >= minDate && u.Date <= maxDate)
                .ToList();

            var newClosePrices = Entities
               .Except(dbRows, _comparer)
               .ToArray();
                    
            var unchangedClosePrices = 0;
            var updatedClosePrices = 0;
            dbRows.ForEach(r =>
            {
                var q = Entities
                    .Where(u => u.Date == r.Date)
                    .Select(u => new { Date = u.Date, Match = u.ValueEquals(r), Data = u })
                    .FirstOrDefault();
                if (q != null) 
                {
                    if (q.Match)
                        unchangedClosePrices++;
                    else
                    {
                        r.ValueCopy(q.Data);
                        r.AdjustUpdate = today;
                        updatedClosePrices++;
                    }
                }
            }
            );

            Parallel.ForEach(newClosePrices, np => { np.CreatedDate = today; np.AdjustUpdate = today; });
            _context.AddRange(newClosePrices);
            _context.SaveChanges();

            return new UpsertStat { NewRows = newClosePrices.Length, UpdatedRows = updatedClosePrices, UnchangedRows = unchangedClosePrices, TotalRows = _context.UnderlyingPrices.Where(u => tickers.Contains(u.Ticker)).Count() };
        }

        public UpsertStat Upsert(UnderlyingPrice Object)
        {
            throw new NotImplementedException();
        }

        public void Delete(UnderlyingPrice Entity)
        {
            if (Entity != null)
                _context.UnderlyingPrices.Remove(Entity);
        }
    }
}
