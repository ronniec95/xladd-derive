using System.Collections.Generic;
using System.Linq;
using AARC.Model;
using AARC.Model.Interfaces.RDS;
using AARC.RDS;
using aarcYahooFinETL.DataSource;
using aarcYahooFinETL.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace aarcYahooFinETL.Controllers
{
    [Route("api/[controller]")]
    public class StocksController : AarcEntityController<Dictionary<string, string>, Stock>
    {
        public StocksController(AARCContext context, IStockQuoteDataSourceClient client, IBackgroundTaskQueue queue, IServiceScopeFactory serviceScopeFactory, ILogger<Controller> logger)
            : base(context, client, queue, serviceScopeFactory, logger)
        {
        }

        public override IEnumerable<string> Get()
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var repository = scopedServices.GetRequiredService<IRepository<Stock>>();

                var stocks = repository.Get();
                if (stocks != null)
                    return stocks.Select(s => s.Ticker);

                return null;
            }
        }

        [Route("load/{id}")]
        public string Load(string id)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var repository = scopedServices.GetRequiredService<IRepository<Stock>>();

                var stocks = repository.Get();
                if (stocks != null)
                {
                    foreach(var ticker in stocks.Select(s => s.Ticker))
                    {

                    }
                }
            }
            return id;
        }

        public override IEnumerable<Stock> Convert(string id, string message)
        {
            var stockinfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);
#if DEBUG
            _logger.LogDebug(message);
#endif
            var stock = new Stock
            {
                Ticker = id.ToUpper(),
                Description = id.ToUpper()
            };
            stock.ToStock(stockinfo);

            return new Stock[] { stock };
        }

        protected Stock ToStock(Dictionary<string, string> dic)
        {
            var stock = new Stock();
            foreach (var kv in dic)
            {
                switch(kv.Key)
                {
                    case @"Market Cap": stock.MarketCap = StockInfo.ToDouble(kv.Value);
                        break;
                    case @"Forward Dividend & Yield":
                    case @"Earnings Date":
                    case @"Beta (3Y Monthly)":
                    case @"PE Ratio (TTM)":
                    case @"EPS (TTM)":
                    case @"Ex-Dividend Date":
                    default:
                        break;
                }
            }
            return stock;
        }
    }
}
