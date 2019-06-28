using System;
using System.Collections.Generic;

namespace aarcweb.Interfaces.Injection
{
    public interface IPortfolioStart
    {
        iPortfolio Portfolio {get;}
        Dictionary<string, string> Options { get; }
    }
}
