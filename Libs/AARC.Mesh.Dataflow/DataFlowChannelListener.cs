using System;
using System.Collections.Generic;
using System.Threading;
using Microsoft.Extensions.Logging;

using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Model.Interfaces;
using System.Collections.Concurrent;
using AARC.Model;
using System.Linq;

namespace AARC.Mesh.Dataflow
{
    public class DataFlowChannelListener : IMeshReactor<MeshMessage>
    {
        private readonly ILogger<DataFlowChannelListener> _logger;
        private readonly ManualResetEvent _marketLoaded;
        private readonly ConcurrentDictionary<string, IAarcPrice> _marketPrices;

        public string Name { get { return "nasdaq"; } }
        public DataFlowChannelListener(ILogger<DataFlowChannelListener> logger)
        {
            _logger = logger;
            _marketPrices = new ConcurrentDictionary<string, IAarcPrice>();
            _marketLoaded = new ManualResetEvent(false);

            ChannelRouters = new List<IRouteRegister<MeshMessage>>();

            ChannelRouters.Add(CreateChannel<TickerPrices>("nasdaqtestout"));
            ChannelRouters.Add(CreateChannel<List<Stock>>("biggeststocks"));

        }

        private IMeshObservable<T> CreateChannel<T>(string channel)
        {
            var observerable = new MeshObservable<T>(channel);

            observerable.Subscribe((value) =>
            {
                _logger.LogInformation($"{channel}: Received an update {value}");
                if (value is TickerPrices)
                {
                    var prices = value as TickerPrices;
                    _logger.LogInformation($"{channel}: Received an update {prices.Ticker}");
                    if (prices != null)
                        _logger.LogInformation($"{channel}: Dates [{prices.Dates.Max()},{prices.Dates.Min()}");
                }
                else if (value is List<Stock>)
                {
                    foreach(var v in value as List<Stock>)
                    {
                        _logger.LogInformation($"{channel}: Received an update {v}");
                    }
                }
                else if (value is List<string>)
                {
                    foreach (var v in value as List<string>)
                    {
                        _logger.LogInformation($"{channel}: Received an update {v}");
                    }
                }
            });
            return observerable;
        }

        public IList<IRouteRegister<MeshMessage>> ChannelRouters { get; private set; }

        public void Start()
        {
            try
            {
                // Todo: Probably need a manualevent to hold off clients the connect before we have data
                Update();
            }
            catch (Exception)
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
        }

    }
}