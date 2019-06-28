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

    public class MarketDataRepository : IMarketDataRepository
    {
        protected IMarketDataContext _context;

        public MarketDataRepository(AARCContext context)
        {
            _context = (IMarketDataContext)context;
        }

        public IDictionary<string, IAarcPrice> GetClosingPrices(IList<string> tickers, uint from, uint to)
        {
            var s = DateTimeUtilities.ToDate(from);
            var e = DateTimeUtilities.ToDate(to);
            
            var query = _context.UnderlyingPrices
                .AsNoTracking()
                .Where(u => tickers.Contains(u.Ticker) && u.Date >= s && u.Date <= e)
                .GroupBy(u => u.Ticker)
                .ToDictionary(g => g.Key, g => g.OrderBy(u => u.Date).ToList());

            var q = query
                .Select(d => new TickerPrices
            {
                Ticker = d.Key,
                Volume = d.Value.Select(v => v.Volume ?? 0.0).ToList(),
                Dates = d.Value.Select(v => DateTimeUtilities.ToUnsignedInt(v.Date)).ToList(),
                ClosingPrices = d.Value.Select(v => v.AdjustedClose ?? v.Close).ToList(),
                High = d.Value.Select(v => v.High ?? 0).ToList(),
                Low = d.Value.Select(v => v.Low ?? 0).ToList(),
                Open = d.Value.Select(v => v.Open ?? 0).ToList()
                })
            .ToDictionary(sp => sp.Ticker, sp => (IAarcPrice)sp);

            return q;
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
