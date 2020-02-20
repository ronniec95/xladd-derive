using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Model;
using AARC.Repository.Interfaces;

namespace AARC.Mesh.Dataflow
{
    public class BiggestStocksReactor : IMeshReactor<MeshMessage>
    {
        private readonly IStockRepository _stocksRepository;
        private readonly object _sync = new object();
        private readonly MeshObserver<List<Stock>> _observer;
        private readonly ILogger<BiggestStocksReactor> _logger;
        private List<Stock> stocks = null;

        public BiggestStocksReactor(ILogger<BiggestStocksReactor> logger, IStockRepository stocksRepository)
        {
            _logger = logger;
            _stocksRepository = stocksRepository;
            _observer = new MeshObserver<List<Stock>>("biggeststocks");

            ChannelRouters = new List<IRouteRegister<MeshMessage>> { _observer as IRouteRegister<MeshMessage>};


            _observer.OnConnect += ((transportUrl) =>
            {
                lock(_sync)
                    if (stocks != null)
                        _observer?.OnNext(stocks);
            });
        }

        public void UpdateBiggestStocks()
        {
            if (stocks == null)
                lock (_sync)
                    stocks = _stocksRepository
                        .GetAllStocks()
                        //.GetStocksByWithOptionsMarketCap()
                        .ToList();


            lock (_sync)
                _observer?.OnNext(stocks);
        }

        public void Start()
		{
			UpdateBiggestStocks();
		}

        public string Name => "biggeststocks";

        public IList<IRouteRegister<MeshMessage>> ChannelRouters { get; private set; }
    }
}
