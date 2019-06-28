using System;
using System.Collections.Generic;
using System.Linq;
using AARC.RDS;
using aarcYahooFinETL.DataSource;
using aarcYahooFinETL.Utilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace aarcYahooFinETL.Controllers
{
    public class AarcEntityController<D, E> : Controller
                                            where E : new()
    {
        protected readonly ILogger _logger;
        public IBackgroundTaskQueue Queue { get; }
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected IStocksDataContext _context;
        protected IAarcWebDataClient _dataSourceClient;

        public AarcEntityController(AARCContext context, IAarcWebDataClient dataSourceClient, IBackgroundTaskQueue queue, IServiceScopeFactory serviceScopeFactory, ILogger<Controller> logger)
        {
            _context = (IStocksDataContext)context;
            _dataSourceClient = dataSourceClient;
            Queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        // GET: api/values
        [HttpGet]
        public virtual IEnumerable<string> Get()
        {
            return new string[] { "Entity Controller" };
        }

        // GET api/values/5
        [HttpGet("{id}")]
        public string Get(string id)
        {
            Queue.QueueBackgroundWorkItem(async token =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    try
                    {
                        var dataRequest = _dataSourceClient.Request(id);
                        await dataRequest;

                        var scopedServices = scope.ServiceProvider;
                        var repository = scopedServices.GetRequiredService<AARC.Model.Interfaces.RDS.IRepository<E>>();

                        var message = dataRequest.Result;

                        var entities = Convert(id, message);
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
                    $"Queued Background Task {id} is complete.");
            });
            return "success";
        }

        public virtual IEnumerable<E> Convert(string id, string message)
        {
            var ups = JsonConvert.DeserializeObject<List<D>>(message);
#if DEBUG
            _logger.LogDebug(message);
#endif
            return ups.Select(u => (E)Activator.CreateInstance(typeof(E), u));
        }
    }
}