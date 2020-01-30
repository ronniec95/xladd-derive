using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AARC.Model.Interfaces.RDS;
using aarcYahooFinETL.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace aarcYahooFinETL.Controllers
{
    public interface IAarcClientService
    {
        Task<string> Request(string value);
    }

    public class aarcService<TClient, R, D> where TClient : IAarcClientService
                                            where R : IRepository<D>
    {
        public IBackgroundTaskQueue Queue { get; }
        protected readonly ILogger _logger;
        protected readonly IServiceScopeFactory _serviceScopeFactory;
        protected TClient _client;


        public aarcService(TClient client, IBackgroundTaskQueue queue, IServiceScopeFactory serviceScopeFactory, ILogger<aarcService<TClient, R, D>> logger)
        {
            _client = client;
            Queue = queue;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public string Get(string id)
        {
            Queue.QueueBackgroundWorkItem(async token =>
            {
                using (var scope = _serviceScopeFactory.CreateScope())
                {
                    var dataRequest = _client.Request(id);
                    await dataRequest;

                    var scopedServices = scope.ServiceProvider;
                    var repository = scopedServices.GetRequiredService<R>();

                    try
                    {
                        var message = dataRequest.Result;
                        var ups = JsonConvert.DeserializeObject<List<D>>(message);
                        repository.Upsert(ups);
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
