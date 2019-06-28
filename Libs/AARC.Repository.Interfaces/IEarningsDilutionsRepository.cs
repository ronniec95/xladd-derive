using System;
using System.Collections.Generic;
using AARC.Model;

namespace AARC.Repository.Interfaces
{
    public interface IEarningsDilutionsRepository
    {
        void Overwrite(IEnumerable<EarningsDiluted> earningsDiluted);
        bool HasDate(string symbol, DateTime date);
    }
}
