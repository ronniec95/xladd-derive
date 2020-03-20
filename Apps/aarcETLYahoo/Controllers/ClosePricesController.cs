using System;
using System.Collections.Generic;
using aarcYahooFinETL.DataSource;
using aarcYahooFinETL.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace aarcYahooFinETL.Controllers
{
    using System.Linq;
    using System.Threading.Tasks;
    using AARC.Model.Interfaces.RDS;
    using AARC.RDS;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Routing;

    [Route("api/[controller]")]
    public class ClosePricesController : Controller
    {
        public IBackgroundTaskQueue Queue { get; }
        protected readonly ILogger _logger;
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected IClosePriceDataContext _context;
        protected IClosingPricesClient _client;

        public ClosePricesController(AARCContext context, IClosingPricesClient closingPricesClient, IBackgroundTaskQueue queue, IServiceScopeFactory serviceScopeFactory, ILogger<ClosePricesController> logger)
        {
            _context = (IClosePriceDataContext)context;
            _client = closingPricesClient;
            Queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        // GET: api/values
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "test ClosePrices" };
        }

        // GET api/ClosePrices/5
        [HttpGet("{id}")]
        public string Get(string id)
        {
            ProcessUpdate(id, GetRequestId);
            return "success";
        }

        // POST api/ClosePrices
        // json list of ticker strings
        [HttpPost]
        public string GetListFromBody([FromBody] List<string> tickers)
        {
            if (tickers != null)
            {
                // Connection Pool is 100

                ProcessUpdates(tickers);
                return $"changes {tickers.Count()}";
            }

            return "NOTHING RECIEVED...";
        }

        protected DateTime? GetMaxDate(string ticker)
        {
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var repository = scopedServices.GetRequiredService<IRepository<AARC.Model.UnderlyingPrice>>();

                var date = repository.GetMaxDate(ticker);
                return date;
            }
        }
        protected IEnumerable<T> GetChanges<T>(IEnumerable<string> tickers) where T : new()
        {
            var changes = new List<T>();
            foreach (var ticker in tickers)
            {
                try
                {
                    var entities = GetChanges<T>(ticker);

                    changes.AddRange(entities);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Problem with [{ticker}] - ignoring");
                    _logger.LogError(ex.Message);
                }
            }
            return changes;
        }

        protected IEnumerable<T> GetChanges<T>(string ticker) where T : new()
        {
            IEnumerable<T> entities = null;
            try
            {
                var date = GetMaxDate(ticker);

                Task<string> dataRequest;

                if (date.HasValue)
                    dataRequest = _client.Request($"{ticker}/{date:yyyyMMdd}/{DateTime.Today:yyyyMMdd}");
                else
                    dataRequest = _client.Request(ticker);

                var message = dataRequest.Result;

                entities = JsonConvert.DeserializeObject<List<T>>(message, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, $"GetChanges {ticker}");
            }
            return entities;
        }

        protected void ProcessUpdates(IEnumerable<string> tickers)
        {
            var requestId = GetRequestId;

            Queue.QueueBackgroundWorkItem(async token =>
            await Task.Run(() =>
            {
                Parallel.ForEach(tickers.Distinct(), new ParallelOptions { MaxDegreeOfParallelism = 80 },
                (ticker) =>
                {
                    _logger.LogInformation($"Process {ticker}");
                    ProcessUpdate(ticker?.Trim(), requestId);
                    _logger.LogInformation($"Complete {ticker}");
                });
                _logger.LogInformation($"LOAD Complete ");
            }));
        }

        protected void ProcessUpdate(string ticker, string requestId)
        {
            if (string.IsNullOrEmpty(ticker))
                return;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var repository = scopedServices.GetRequiredService<IRepository<AARC.Model.UnderlyingPrice>>();
                var notifyRepository = scopedServices.GetRequiredService<IRepository<AARC.Model.ServiceStats>>();
                IEnumerable<AARC.Model.UnderlyingPrice> changes = null;
                UpsertStat stats = null;
                var serviceStats = new AARC.Model.ServiceStats { Service = GetRequestPath, Instance = requestId + ticker, Symbol = ticker, Status = @"START" };
   //             using (var transaction = _context.Database.BeginTransaction())
                {
                    try
                    {
                        serviceStats.Start = DateTime.Now;
                        notifyRepository.Upsert(serviceStats);
                        changes = GetChanges<AARC.Model.UnderlyingPrice>(ticker);
                        if (changes != null)
                        {
                            serviceStats.NewRows = (changes?.Count() ?? 0);
                            serviceStats.Status = @"PROCESSING";
                            notifyRepository.Upsert(serviceStats);
                            stats = repository.Upsert(changes);
                            if (stats != null)
                            {
                                serviceStats.NewRows = stats.NewRows;
                                serviceStats.UpdatedRows = stats.UpdatedRows;
                                serviceStats.UnchangedRows = stats.UnchangedRows;
                                serviceStats.TotalRows = stats.TotalRows;
                            }
                        }
                        else serviceStats.Message = "No Data from Source";
                        serviceStats.Status = @"COMPLETE";
                    }
                    catch (Exception ex)
                    {
                        serviceStats.Message = ex.Message;
                        serviceStats.Status = @"ERROR";
                        _logger.LogError($"Batch {string.Join(",", ticker)} {changes?.Count()}");
                        _logger.LogError(ex, "An error occurred writing to the database. Error: " + ex.Message);
                    }
                    finally
                    {
                        notifyRepository.Upsert(serviceStats);
                        //  transaction.Commit();
                    }
                }
            }
        }

        protected string GetRequestId => HttpContext.TraceIdentifier;
        protected string GetCurrentUser => HttpContext.User.Identity.Name;

        protected string GetRequestPath => "/api/ClosePrices/";
    }
}