using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace AARC.Repository.EF
{
    using AARC.Model;
    using AARC.Model.Interfaces;
    using AARC.Utilities;
    using AARC.RDS;
    using AARC.Repository.Interfaces;
    using System.Collections.Concurrent;

    public class MarketDataRepository : IMarketDataRepository
    {
        protected IMarketDataContext _context;

        public MarketDataRepository(AARCContext context)
        {
            _context = (IMarketDataContext)context;
        }

        public Dictionary<string, TickerPrices> GetClosingPrices(IList<string> tickers, uint from, uint to)
        {
            var closingPrices = new Dictionary<string, TickerPrices>();
            var s = DateTimeUtilities.ToDate(from);
            var e = DateTimeUtilities.ToDate(to);

            foreach (var ticker in tickers)
            {
                var query = _context.UnderlyingPrices
                    .AsNoTracking()
                    .Where(u => u.Ticker == ticker)
                    .Where(u => u.Date >= s && u.Date <= e)
                    .ToList();

                var tp = new TickerPrices
                {
                    Ticker = ticker,
                    Volume = query.Select(v => v.Volume ?? 0.0).ToList(),
                    Dates = query.Select(v => DateTimeUtilities.ToUnsignedInt(v.Date)).ToList(),
                    ClosingPrices = query.Select(v => v.AdjustedClose ?? v.Close).ToList(),
                    High = query.Select(v => v.High ?? 0).ToList(),
                    Low = query.Select(v => v.Low ?? 0).ToList(),
                    Open = query.Select(v => v.Open ?? 0).ToList()
                };
                closingPrices.TryAdd(ticker, tp);
            }
            return closingPrices;
        }

        public IDictionary<string, IList<double>> GetClosingPrices(string ticker, uint from, uint to)
        {
            var s = DateTimeUtilities.ToDate(from);
            var e = DateTimeUtilities.ToDate(to);

            var closeprices = _context.UnderlyingPrices
                .AsNoTracking()
                .Where(u => u.Ticker == ticker && u.Date >= s && u.Date <= e)
                .Select(v => v.AdjustedClose ?? v.Close)
                .ToList();

            var data = new Dictionary<string, IList<double>>();
            data.Add(ticker, closeprices);
            return data;
        }

        public string GetInfo() => this.GetType().Name;

        public IDictionary<string, IAarcInstrument> GetInfo(IList<string> tickers)
        {
            return _context.Stocks
                .AsNoTracking()
                .Where(u => tickers.Contains(u.Ticker))
                .ToDictionary(s => s.Ticker, s => (IAarcInstrument)s);
        }

        public IList<string> GetTickers()
        {
            return _context.Stocks.AsNoTracking().Select(s => s.Ticker).ToList();
        }

        public IList<string> GetStocksByMarketCap(double minMarketCap, double maxMarketCap)
        {
            return _context.Stocks
                .AsNoTracking()
                .Where(s => s.MarketCap >= minMarketCap && s.MarketCap <= maxMarketCap)
                .Select(s => s.Ticker)
                .ToList();
        }

        public IList<Tuple<string, uint>> GetStatus()
        {
            return _context.UnderlyingPrices
                .AsNoTracking()
                .GroupBy(u => u.Ticker)
                .Select(g => new Tuple<string, uint>(g.Key, (uint)g.Count()))
                .ToList();
        }

        public IList<string> GetStocks()
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, IAarcInstrument> GetStockInfo(IList<string> tickers)
        {
            throw new NotImplementedException();
        }
    }
}
