using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.Logging;
using AARC.Model;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Utilities;
using AARC.Repository.Interfaces;
using AARC.Model.Interfaces;

namespace AARC.Mesh.Dataflow
{
    public class NasdaqTradableTickers : IMeshReactor<MeshMessage>
    {
        public const string NasdaqTicker = @"^IXIC";
        private readonly IMarketDataRepository _marketDataRepository;
        private Dictionary<string, TickerPrices> _marketUniverse;
        private readonly object _sync = new object();
        private readonly HashSet<string> _tickers;
        private readonly ILogger<NasdaqTradableTickers> _logger;
        private readonly MeshObservable<List<string>> observerable;
        private readonly MeshObserver<IAarcPrice> observer;
        private readonly ManualResetEvent _marketLoaded;

        public string Name { get { return "nasdaq"; } }
        public NasdaqTradableTickers(ILogger<NasdaqTradableTickers> logger, IMarketDataRepository marketDataRepository)
        {
            _logger = logger;
            _tickers = new HashSet<string>();
            _tickers.Add(NasdaqTicker);
            _tickers.Add("AAPL");
            _marketDataRepository = marketDataRepository;
            _marketLoaded = new ManualResetEvent(false);

            observerable = new MeshObservable<List<string>>("nasdaqtestin");
            observer = new MeshObserver<IAarcPrice>("nasdaqtestout");

            ChannelRouters = new List<IRouteRegister<MeshMessage>> { observer as IRouteRegister<MeshMessage>, observerable as IRouteRegister<MeshMessage> };

            observerable.Subscribe((tickers) =>
                {
                    _logger.LogInformation($"Received an update request {string.Join("", tickers)}");
                    // Should update by Ticker
                    _tickers.Union(tickers);
                    Update();
                });

            observer.OnConnect += (transportUrl) =>
            {
                _marketLoaded.WaitOne();
                lock (_sync)
                    if (_marketUniverse != null)
                        foreach (var kvp in _marketUniverse)
                            observer?.OnNext(kvp.Value, transportUrl);
            };
        }

        public IList<IRouteRegister<MeshMessage>> ChannelRouters { get; private set; }

        public void Start()
        {
            try
            {
                // Todo: Probably need a manualevent to hold off clients the connect before we have data
                Update();
            }
            catch(Exception)
            {
                throw;
            }
            finally
            {
                _marketLoaded.Set();
            }
        }

        private void Update()
        {
            Dictionary<string, TickerPrices> changes;

            lock(_marketDataRepository)
                changes= _marketDataRepository?.GetClosingPrices(_tickers.ToArray(), 19700101, DateTime.Now.ToUYYYYMMDD());

            foreach (var kvp in changes)
                   observer?.OnNext(kvp.Value);

            lock (_sync)
                if (_marketUniverse != null)
                    _marketUniverse = changes
                 .Concat(_marketUniverse)
                 .GroupBy(i => i.Key)
                 .ToDictionary(
                     group => group.Key,
                     group => group.First().Value); // Take the results from changes
                else _marketUniverse = changes;
        }

    }
}
