using System.Collections.Generic;
using AARC.Model.Interfaces;
using AARC.Repository.Interfaces;
using log4net;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace aarcweb.Controllers
{
    [Route("api/[controller]")]
    public class HistoryController : Controller
    {
        private ILog _logger = LogManager.GetLogger(typeof(EODController));
        private readonly IMarketDataRepository _marketDataRepository;

        public HistoryController(IMarketDataRepository marketDataProvider)
        {
            _logger.Debug($"{this.GetType().Name} created {marketDataProvider?.GetType().Name} provided");
            _marketDataRepository = marketDataProvider;
        }

        // GET: api/values
        [HttpGet]
        public HistoryRequest Get()
        {
            return new HistoryRequest
            {
                tickers = new List<string> { "AAPL", "MSFT" },
                startDate = 20180101,
                endDate = 20190101
            };
        }

        // GET api/values/5
        [HttpGet("{id}/{startDate}/{endDate}")]
        public IDictionary<string, IList<double>> Get(string id, uint startDate, uint endDate) => _marketDataRepository.GetClosingPrices(id, startDate, endDate);

        // GET api/values/5
        [HttpPost("")]
        public IDictionary<string, IAarcPrice> Post([FromBody] HistoryRequest request)
        {
            return _marketDataRepository.GetClosingPrices(request.tickers, request.startDate, request.endDate);
        }

        public class HistoryRequest
        {
            public IList<string> tickers { get; set; }
            public uint startDate { get; set; }
            public uint endDate { get; set; }
        }
    }
}
