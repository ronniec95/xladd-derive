using System.Collections.Generic;

namespace AARC.Repository.Interfaces
{
    public interface IIndexStockRepository
    {
        IList<string> GetWeeklyOptions(string indexName);
    }
}
