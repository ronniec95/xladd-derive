using System;
using System.Collections.Generic;
using AARC.Model;

namespace AARC.Repository.Interfaces
{
    public interface IOptionsRepository
    {
        Option GetOption(Option option, DateTime date);

        OptionChainsBase GetOptions(string ticker, DateTime date, double underlyingClose);

        bool HasDate(string symbol, DateTime date);

        void Overwrite(IList<Option> options);
    }
}
