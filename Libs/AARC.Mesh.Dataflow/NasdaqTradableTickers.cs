using System;
using System.Collections.Generic;
using Newtonsoft.Json;

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
        private IList<IMeshObserver<IDictionary<string, IAarcPrice>>> _observers;
        private IList<IMeshObservable<IList<string>>> _observerables;
        private readonly HashSet<string> _tickers;
        private readonly ILogger<NasdaqTradableTickers> _logger;

        public string Name { get { return "nasdaq"; } }
        public NasdaqTradableTickers(ILogger<NasdaqTradableTickers> logger, IMarketDataRepository marketDataRepository, IMeshObserver<IDictionary<string, IAarcPrice>> observer, IMeshObservable<IList<string>> observerable)
        {
            _logger = logger;
            _tickers = new HashSet<string>();
            _tickers.Add(NasdaqTicker);
            _tickers.Add("AAPL");
            _marketDataRepository = marketDataRepository;
            _observers = new List<IMeshObserver<IDictionary<string, IAarcPrice>>> { observer };
            _observerables = new List<IMeshObservable<IList<string>>> { observerable };

            Queues = new List < IRouteRegister < MeshMessage >> { observer as IRouteRegister<MeshMessage>, observerable as IRouteRegister<MeshMessage> };

            foreach (var observable in _observerables)
                observable.Subscribe((tickers) =>
                {
                    _logger.LogInformation($"Received an update request {string.Join("","", tickers)}");
                    // Should update by Ticker
                    _tickers.Union(tickers);
                    Update();
                    Post();
                });
            Update();
            Post();
        }

        public IList<IRouteRegister<MeshMessage>> Queues { get; private set; }

                
        private void Update()
        {
            lock (_sync)
                _marketUniverse = _marketDataRepository?.GetClosingPrices(_tickers.ToArray(), 19700101, DateTime.Now.ToUYYYYMMDD());
        }

        private void Post()
        {
            lock (_sync)
            {
                foreach(var observer in _observers)
                    observer?.OnNext(_marketUniverse);
            }
        }
    }
}
