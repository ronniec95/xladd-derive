using System;
using System.Collections.Generic;
using System.Linq;
using AARC.Model;
using AARC.Model.Interfaces.RDS;
using AARC.RDS;
using Microsoft.Extensions.Logging;

namespace AARC.Repository.EF
{
    public class ServiceStatsRepository : IRepository<ServiceStats>
    {
        IServiceStatsContext _context;
        ILogger<ServiceStats> _logger;

        public ServiceStatsRepository(AARCContext context, ILogger<ServiceStats> logger)
        {
            _context = context;
            _logger = logger;
        }

        public IEnumerable<ServiceStats> Get()
        {
            throw new NotImplementedException();
        }

        public DateTime? GetMaxDate(string ticker)
        {
            throw new NotImplementedException();
        }

        public UpsertStat Upsert(ServiceStats Entity)
        {
            var existingRow = _context.ServiceStats.Where(o => o.Instance == Entity.Instance).FirstOrDefault();
            if (existingRow != null)
            {
                existingRow.Status = Entity.Status;
                existingRow.Message = Entity.Message;
                existingRow.Updated = DateTime.Now;
            }
            else
            {
                Entity.Updated = DateTime.Now;
                _context.Add(Entity);
            }
            _context.SaveChanges();

            return new UpsertStat();
        }

        public UpsertStat Upsert(IEnumerable<ServiceStats> Entities)
        {
            throw new NotImplementedException();
        }

        public class ServiceStatsComparer : IEqualityComparer<AARC.Model.ServiceStats>
        {
            public bool Equals(AARC.Model.ServiceStats x, AARC.Model.ServiceStats y)
            {
                return x.Symbol == y.Symbol
                    && x.Instance == y.Instance
                    && x.NewRows == y.NewRows
                    && x.UnchangedRows == y.UnchangedRows
                    && x.UpdatedRows == y.UpdatedRows;
            }

            public int GetHashCode(AARC.Model.ServiceStats obj)
            {
                //Check whether the object is null
                if (obj is null) return 0;

                //Get hash code for the Name field if it is not null.
                int hashSymbol = obj.Symbol == null ? 0 : obj.Symbol.GetHashCode();

                int hashInstance = obj.Instance == null ? 0 : obj.Instance.GetHashCode();

                //Calculate the hash code for the obj.
                return hashSymbol ^ hashInstance;
            }
        }
    }
}
