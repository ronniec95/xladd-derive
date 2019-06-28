using System;
using System.Collections.Generic;
using AARC.Model;

namespace AARC.Repository.Interfaces
{
    public interface IDividendRepository
    {
        void Add(Dividend dividend);
        void Overwrite(List<Dividend> dividends);
        DateTime Max(string symbol);
    }
}
