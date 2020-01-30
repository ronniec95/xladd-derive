using System;
using System.Collections.Generic;

namespace aarcweb.Interfaces.Injection
{
    public interface iPortfolio
    {
        IList<Tuple<string, int>> TickerAllocation { get; set; }
    }
}
