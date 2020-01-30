using System;
namespace aarcweb.Interfaces.Injection
{
    public interface IStockInfo
    {
        string Ticker { get; set; }
        double MarketCap { get; set; }
    }
}
