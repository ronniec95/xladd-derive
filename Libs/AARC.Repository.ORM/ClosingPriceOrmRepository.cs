using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AARC.Model;
using AARC.Model.Interfaces.RDS;
using AARC.RDS;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AARC.Repository.ORM
{
    public class ClosingPriceOrmRepository : IRepository<UnderlyingPrice>
    {
        IClosePriceDataContext _context;
        ILogger<ClosingPriceOrmRepository> _logger;
        IEqualityComparer<AARC.Model.UnderlyingPrice> _comparer;
        OrmSql<UnderlyingPrice> _ormHelper;

        public ClosingPriceOrmRepository(AARCContext context, IEqualityComparer<AARC.Model.UnderlyingPrice> comparer, ILogger<ClosingPriceOrmRepository> logger)
        {
            _context = context;
            _comparer = comparer; // new Comparers.TickerDateComparer()
            _logger = logger;

            _ormHelper = new OrmSql<UnderlyingPrice>("UnderlyingPrices");

        }

        public IEnumerable<UnderlyingPrice> Get()
        {
            using (var conn = _context.Database.GetDbConnection())
                return conn.Query<UnderlyingPrice>(_ormHelper.SelectSql);
        }

        public DateTime? GetMaxDate(string ticker)
        {
            using (var conn = _context.Database.GetDbConnection())
                return conn.ExecuteScalar<DateTime?>("select max([date]) from UnderlyingPrices");
        }

        public IEnumerable<double> GetClosePrices(string ticker, DateTime startDate, DateTime endDate)
        {
            using (var conn = _context.Database.GetDbConnection())
                return conn.Query<double>($"select COALESCE([AdjustClose], [Close]) from UnderlyingPrices where [Ticker]='{ticker}' and [Date]>>='{startDate:dd-MMM-yyyy}' and [Date]<='{endDate:dd-MMM-yyyy}'");
        }

        public IEnumerable<dynamic> GetWeekSummary()
        {
            using (var conn = _context.Database.GetDbConnection())
                return conn.Query(
@"select [Ticker],[Mon],[Tue],[Wed],[Thu],[Fri] from
(
  select [Ticker],left(DATENAME(WEEKDAY,[Date]), 3) [Day], AdjustedClose [Rows]
  from UnderlyingPrices
  where [Date] >=  DATEADD(DD,-7,GETDATE())
) src
pivot
(
  Sum([Rows])
  for [Day] in ([Mon],[Tue],[Wed],[Thu],[Fri])
) piv
Order by Ticker"
);
        }

        public UpsertStat Upsert(UnderlyingPrice Object)
        {
            throw new NotImplementedException();
        }

        public UpsertStat Upsert(IEnumerable<UnderlyingPrice> Entities)
        {
            var today = DateTime.Now;
            var maxDate = Entities.Max(u => u.Date);
            var minDate = Entities.Min(u => u.Date);
            var tickers = Entities.Select(e => e.Ticker).Distinct();
            var ticker = Entities.Select(u => u.Ticker).FirstOrDefault();

            using (var conn = _context.Database.GetDbConnection())
            {
                var sql = $"{_ormHelper.SelectSql} where [Ticker]='{ticker}' and [Date]>='{minDate:dd-MMM-yyyy}' and [Date]<='{maxDate:dd-MMM-yyyy}'";
                var dbRows = conn.Query<UnderlyingPrice>(sql)
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
                            conn.Execute(_ormHelper.UpdateSql, r);
                            updatedClosePrices++;
                        }
                    }
                }
                );


                Parallel.ForEach(newClosePrices, np => { np.CreatedDate = today; np.AdjustUpdate = today; });
                conn.Execute(_ormHelper.InsertSql, newClosePrices);

                return new UpsertStat { NewRows = newClosePrices.Length, UpdatedRows = updatedClosePrices, UnchangedRows = unchangedClosePrices, TotalRows = _context.UnderlyingPrices.Where(u => tickers.Contains(u.Ticker)).Count() };
            }
        }
    }
}
