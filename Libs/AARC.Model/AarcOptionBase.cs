using System;

namespace AARC.Model
{
    public interface AarcOptionBase
    {
        string Ticker { get; set; }
        string LocalSymbol { get; }
        DateTime Date { get; set; }
        DateTime Expiry { get; set; }
        double Strike { get; set; }
        string PutCall { get; set; }
    }
}
