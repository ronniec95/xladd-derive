using System.Collections.Generic;
using AARC.Model;
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
    public class StatsController : AarcEntityController<StockInfo, Stock>
    {
        public StatsController(AARCContext context, IStockStatsDataSourceClient client, IBackgroundTaskQueue queue, IServiceScopeFactory serviceScopeFactory, ILogger<Controller> logger)
            : base(context, client, queue, serviceScopeFactory, logger)
        {
        }

        public override IEnumerable<Stock> Convert(string id, string message)
        {
            var stockinfo = JsonConvert.DeserializeObject<List<StockInfo>>(message);
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
    }
}
