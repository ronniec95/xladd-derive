using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    public class LoadController : Controller
    {
        public IBackgroundTaskQueue Queue { get; }
        protected readonly ILogger _logger;
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected IStocksDataContext _context;
        protected IClosingPricesClient _dataSourceClient;

        public LoadController(AARCContext context, IClosingPricesClient dataSourceClient, IBackgroundTaskQueue taskQueue, IServiceScopeFactory serviceScopeFactory, ILogger<Controller> logger)
        {
            _context = (IStocksDataContext)context;
            _dataSourceClient = dataSourceClient;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
            Queue = taskQueue;
        }
         
        [HttpGet]
        public IEnumerable<string> Get()
        {
            var notickers = 0;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var repository = scopedServices.GetRequiredService<IRepository<AARC.Model.Stock>>();

                var stocks = repository.Get();
                if (stocks != null)
                {
                    var tickers = stocks.Select(s => s.Ticker);
                    notickers = tickers.Count();
                    SaveEntityChanges<AARC.Model.Stock>(tickers);
                }
            }

            return new string[] { $"ticker {notickers}" };
        }

        protected IEnumerable<T> GetChanges<T>(IEnumerable<string> tickers) where T : new()
        {
            var stocks = new List<T>();
            foreach (var ticker in tickers)
            {
                var dataRequest = _dataSourceClient.Request(ticker);

                var message = dataRequest.Result;

                var stock = Convert<T>(ticker, message);

                stocks.AddRange(stock);
            }
            return stocks;
        }

        private void SaveEntityChanges<T>(IEnumerable<string> tickers) where T: new()
        {
            Queue.QueueBackgroundWorkItem(async token =>
            await Task.Run(() =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    try
                    {
                        var entities = GetChanges<T>(tickers);
                        var scopedServices = scope.ServiceProvider;
                        var repository = scopedServices.GetRequiredService<IRepository<T>>();

                        repository.Upsert(entities);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "An error occurred writing to the " +
                            $"database. Error: {ex.Message}");
                        throw;
                    }
                }

                _logger.LogInformation(
                    $"Queued Background Task SaveStockChanges is complete.");
            }));
        }

        private static IEnumerable<T> Convert<T>(string id, string message)  where T : new()
        {
            var stockinfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(message);

            T stock = new T();
//            {
//                Ticker = id.ToUpper(),
//                Description = id.ToUpper()
//            };
//            stock.ToStock(stockinfo);

            return new T[] { stock };
        }
    }


}
