using System;
using System.Collections.Generic;
using System.Linq;
using AARC.Model;
using AARC.Model.Interfaces.RDS;
using AARC.RDS;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AARC.Repository.ORM
{
    public class ServiceStatsOrmRepository : IRepository<ServiceStats>
    {
        IServiceStatsContext _context;
        ILogger<ServiceStats> _logger;
        IEqualityComparer<AARC.Model.ServiceStats> _comparer;
        OrmSql<ServiceStats> _ormHelper;

        public ServiceStatsOrmRepository(AARCContext context, IEqualityComparer<AARC.Model.ServiceStats> comparer, ILogger<ServiceStats> logger)
        {
            _context = context;
            _logger = logger;
            _comparer = comparer;
            _ormHelper = new OrmSql<ServiceStats>("ServiceStats");
        }

        public IEnumerable<ServiceStats> Get()
        {
            using (var conn = _context.Database.GetDbConnection())
                return conn.Query<ServiceStats>(_ormHelper.SelectSql);
        }

        public DateTime? GetMaxDate(string ticker)
        {
            throw new NotImplementedException();
        }

        public UpsertStat Upsert(ServiceStats Entity)
        {
            using (var conn = _context.Database.GetDbConnection())
            {
                var sql = $"{_ormHelper.SelectSql} where [Instance]='{Entity.Instance}'";
                var existingRow = conn.ExecuteScalar<ServiceStats>(sql);

                if (existingRow != null)
                {
                    existingRow.Status = Entity.Status;
                    existingRow.Message = Entity.Message;
                    existingRow.Updated = DateTime.Now;
                    conn.Execute(_ormHelper.UpdateSql, Entity);
                }
                else
                {
                    Entity.Updated = DateTime.Now;
                    conn.Execute(_ormHelper.InsertSql, Entity);
                }
            }

            return new UpsertStat();
        }

        public UpsertStat Upsert(IEnumerable<ServiceStats> Entities)
        {
            throw new NotImplementedException();
        }

    }
}
