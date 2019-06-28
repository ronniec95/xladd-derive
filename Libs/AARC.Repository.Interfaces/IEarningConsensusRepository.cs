using System.Collections.Generic;
using AARC.Model;

namespace AARC.Repository.Interfaces
{
    public interface IEarningConsensusRepository
    {
        void Overwrite(IList<EarningsConsensus> earningConsensus);
    }
}
