using System;
using System.Collections.Generic;
using AARC.Model.Interfaces;

namespace AARC.Repository.Interfaces
{
    public interface IFxDataRepository
    {
        /// <summary>
        /// Provides info about the object
        /// </summary>
        /// <returns>The info.</returns>
        string GetInfo();

        /// <summary>
        /// Instrument prices.
        /// </summary>
        /// <returns>ticker, list of dates, closing prices and volumes</returns>
        /// <param name="tickers">Tickers.</param>
        IDictionary<string, IAarcPrice> GetPrices(IList<string> tickers, uint from, uint to);

        /// <summary>
        /// Gets the stocks.
        /// </summary>
        /// <returns>The stocks.</returns>
        IList<string> GetTickers();

        /// <summary>
        /// Gets the stocks by market cap.
        /// </summary>
        /// <returns>The stocks by market cap.</returns>
        /// <param name="minMarketCap">Minimum market cap.</param>
        /// <param name="maxMarketCap">Max market cap.</param>
        IList<string> GetStocksByMarketCap(double minMarketCap, double maxMarketCap);

        /// <summary>
        /// Gets the stock info.
        /// </summary>
        /// <returns>The stock info.</returns>
        /// <param name="tickers">Tickers.</param>
        IDictionary<string, IAarcInstrument> GetInfo(IList<string> tickers);

        /// <summary>
        /// Gets the status.
        /// </summary>
        /// <returns>The status.</returns>
        IList<Tuple<string, uint>> GetStatus();
    }
}
