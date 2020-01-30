using System;
using System.Collections.Generic;

namespace AARC.Model.Interfaces.RDS
{
    public interface IRepository<T>
    {
        IEnumerable<T> Get();
        UpsertStat Upsert(T Object);
        UpsertStat Upsert(IEnumerable<T> Entity);
        DateTime? GetMaxDate(string ticker);
    }
}
