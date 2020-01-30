using System.Collections.Generic;
using AARC.Model;
using AARC.Repository.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace aarcweb.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class StocksController : ControllerBase
    {
        protected IStockRepository _stockDataProvider;
        public StocksController(IStockRepository stockDataProvider)
        {
            _stockDataProvider = stockDataProvider;
        }

        [HttpGet]
        public IEnumerable<Stock> Get() => _stockDataProvider.GetAllStocks();

        [HttpGet("{id}", Name = "Exchange")]
        public IEnumerable<Stock> Exchange(string id) => _stockDataProvider.GetStocksByExchange(id);

        [HttpGet("{id}", Name = "Sector")]
        public IEnumerable<Stock> Sector(string id) => _stockDataProvider.GetStocksBySector(id);

        [HttpGet("{id}", Name = "MarketCap")]
        public IEnumerable<Stock> MarketCap(long id) => _stockDataProvider.GetStocksByGreaterEqualMarketCap(id);

        [HttpGet("{id}")]
        public Stock Get(string id) => _stockDataProvider.GetStock(id);

        [HttpPost]
        public void Post([FromBody] Stock value) => _stockDataProvider.Overwrite(value);

        [HttpDelete("{value}")]
        public void Delete(string value) => _stockDataProvider.Delete(value);
    }
}
