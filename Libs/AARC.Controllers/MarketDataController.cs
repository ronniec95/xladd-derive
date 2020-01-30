using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;

namespace AARC.Controllers
{
    using AARC.Model.Interfaces;
    using AARC.Repository.Interfaces;

    [Route("api/[controller]/[action]")]
    [ApiController]
    public class MarketDataController : ControllerBase
    {
        protected IMarketDataRepository _marketDataRepository;

        public MarketDataController(IMarketDataRepository repository)
        {
            _marketDataRepository = repository;
        }
        // GET: api/MarketData
        [HttpGet]
        public string Get() => _marketDataRepository.GetInfo();

        // GET: api/MarketData/5/19000101/19000102
        [HttpGet("ClosePrices/{id}/from/to")]
        public IDictionary<string, IList<double> > ClosePrices(string id, uint from, uint to) => _marketDataRepository.GetClosingPrices(id, from, to);

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
