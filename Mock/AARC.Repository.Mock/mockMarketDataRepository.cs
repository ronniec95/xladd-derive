using System;
using System.Collections.Generic;
using System.Linq;
using AARC.Model;
using AARC.Model.Interfaces;
using AARC.Repository.Interfaces;

namespace AARC.Repository.Mock
{
    public class mockMarketDataRepository : IMarketDataRepository
    {
        public string GetInfo() => this.GetType().Name;

        public IDictionary<string, TickerPrices> GetClosingPrices(IList<string> tickers, uint from, uint to)
            => tickers
                .ToDictionary(t => t, t => new TickerPrices { Ticker = t, ClosingPrices = new List<double>(), Dates = new List<uint>(), Volume = new List<double>() });

        public IDictionary<string, IAarcInstrument> GetStockInfo(IList<string> tickers)
            => tickers
                .ToDictionary(t => t, t => (IAarcInstrument) new Stock { Ticker = t, MarketCap = -999 });

        public IList<string> GetStocks()
            => new string[] { "APPL", "MSFT" };

        public IList<string> GetStocksByMarketCap(double minMarketCap, double maxMarketCap)
            => GetStocks();

        public IList<Tuple<string, uint>> GetStatus()
            => new List<Tuple<string, uint>> { new Tuple<string, uint>("Test", (uint)DateTime.Now.Year) };

        public IDictionary<string, IList<double>> GetClosingPrices(string ticker, uint from, uint to)
        {
            throw new NotImplementedException();
        }

        IDictionary<string, IAarcPrice> IMarketDataRepository.GetClosingPrices(IList<string> tickers, uint from, uint to)
        {
            throw new NotImplementedException();
        }

        public IList<string> GetTickers()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IAarcInstrument> GetInfo(IList<string> tickers)
        {
            throw new NotImplementedException();
        }
    }
}
