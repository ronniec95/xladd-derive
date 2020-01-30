using System.Collections.Generic;
using AARC.Model;

namespace AARC.Repository.Interfaces
{
    public interface IEarningsRepository
    {
        //void Add(EarningsSurprise earningsSurprice);
        //void AddRange(IEnumerable<EarningsSurprise> earningSurprices);
        void Overwrite(IEnumerable<EarningsSurprise> earningSurprices);
        void Merge(IEnumerable<EarningsSurprise> earningSurprices);
    }
}
