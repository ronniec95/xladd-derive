using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AARC.Model;

namespace AARC.Repository.Interfaces
{
    public interface IEarningsSurprizeRepository
    {
        void Overwrite(IList<EarningsSurprise> earningsSuprizes);
    }
}
