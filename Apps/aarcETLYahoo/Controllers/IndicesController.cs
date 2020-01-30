using System;
using System.Collections.Generic;
using System.Linq;
using aarcYahooFinETL.DataSource;
using aarcYahooFinETL.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace aarcYahooFinETL.Controllers
{
    using AARC.Model.Interfaces.RDS;
    using AARC.RDS;

    [Route("api/[controller]")]
    public class IndicesController : Controller
    {
        protected readonly ILogger _logger;
        public IBackgroundTaskQueue Queue { get; }
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected IStocksDataContext _context;
        protected IIndicesDataSource _dataClient;

        public IndicesController(AARCContext context, IIndicesDataSource dataClient, IBackgroundTaskQueue queue, IServiceScopeFactory serviceScopeFactory, ILogger<ClosePricesController> logger)
        {
            _context = (IStocksDataContext)context;
            _dataClient = dataClient;
            Queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        // GET: api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "indices" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(string id)
        {
            Queue.QueueBackgroundWorkItem(async token =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dataRequest = _dataClient.Request(id);
                    await dataRequest;

                    var scopedServices = scope.ServiceProvider;
                    var repository = scopedServices.GetRequiredService<IRepository<AARC.Model.Stock>>();

                    try
                    {
                        var message = dataRequest.Result;
                        var ups = JsonConvert.DeserializeObject<List<string>>(message);
#if DEBUG
                        _logger.LogDebug(message);
#endif
                        var stocks = ups.Select(u => new AARC.Model.Stock { Ticker = u, Exchange = id.ToUpper(), Description = u });
                        repository.Upsert(stocks);
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
                    $"Queued Background Task {id} is complete.");
            });
            return "success";
        }
    }
}
