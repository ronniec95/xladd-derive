using System;
using System.Collections.Generic;
using System.Linq;
using AARC.Model;
using AARC.RDS;
using AARC.Repository.Interfaces;
using AARC.Utilities;
using Microsoft.EntityFrameworkCore;

namespace AARC.ETL.Repositories
{
    public class DividendRepository : IDividendRepository
    {
        public AARCContext _context;
        public DividendRepository(AARCContext context)
        {
            _context = context;
        }

        public List<Dividend> GetAllDividends()
        {
            return _context.Dividends
                .AsNoTracking()
                .OrderBy(x => x.ExDate)
                .ToList();
        }

        public List<Dividend> GetDividends(uint startDate, uint endDate)
        {
            var s = DateTimeUtilities.ToDate(startDate);
            var e = DateTimeUtilities.ToDate(endDate);
            return _context.Dividends
                .AsNoTracking()
                .Where(x => x.ExDate >= s && x.ExDate <= e)
                .OrderBy(x => x.ExDate)
                .ToList();
        }

        public void Add(Dividend dividend)
        {
            throw new NotImplementedException();
        }

        public void Overwrite(List<Dividend> dividends)
        {
            throw new NotImplementedException();
        }

        public DateTime Max(string symbol)
        {
            throw new NotImplementedException();
        }
    }
}
