using System;
using AARC.Model.Interfaces;
using aarcweb.Interfaces.Injection;

namespace aarcweb.Models.MarketData
{
    public struct StockInfo : IAarcInstrument
    {
        public double MarketCap { get; set; }

        public string Ticker { get; set; }

        public AarcContractType ContractType => throw new NotImplementedException();

        public string LocalSymbol => $"{Ticker}";
    }
}