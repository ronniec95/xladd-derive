using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Utilities;
using AARC.Repository.Interfaces;
using AARC.Model.Interfaces;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.Dataflow
{
    public class NasdaqTradableTickers : IMeshReactor<MeshMessage>
    {
        public const string NasdaqTicker = @"^IXIC";
        private readonly IMarketDataRepository _marketDataRepository;
        private IDictionary<string, IAarcPrice> _marketUniverse;
        private readonly object _sync = new object();
        private IList<IMeshObserver<IAarcPrice>> _observers;
        private IList<IMeshObservable<List<string>>> _observerables;
        private readonly HashSet<string> _tickers;
        private readonly ILogger<NasdaqTradableTickers> _logger;

        public string Name { get { return "nasdaq"; } }
        public NasdaqTradableTickers(ILogger<NasdaqTradableTickers> logger, IMarketDataRepository marketDataRepository, IMeshObserver<IAarcPrice> observer, IMeshObservable<List<string>> observerable)
        {
            _logger = logger;
            _tickers = new HashSet<string>();
            _tickers.Add(NasdaqTicker);
            _tickers.Add("AAPL");
            _marketDataRepository = marketDataRepository;
            _observers = new List<IMeshObserver<IAarcPrice>> { observer };
            _observerables = new List<IMeshObservable<List<string>>> { observerable };

            ChannelRouters = new List < IRouteRegister < MeshMessage >> { observer as IRouteRegister<MeshMessage>, observerable as IRouteRegister<MeshMessage> };

            foreach (var observable in _observerables)
                observable.Subscribe((tickers) =>
                {
                    _logger.LogInformation($"Received an update request {string.Join("", tickers)}");
                    // Should update by Ticker
                    _tickers.Union(tickers);
                    Update();
                });
            Update();
        }

        public IList<IRouteRegister<MeshMessage>> ChannelRouters { get; private set; }

        private void Update()
        {
            var changes = _marketDataRepository?.GetClosingPrices(_tickers.ToArray(), 19700101, DateTime.Now.ToUYYYYMMDD());

            foreach (var kvp in changes)
                    foreach (var observer in _observers)
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
