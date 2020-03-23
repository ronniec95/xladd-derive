using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace aarcYahooFinETL.Utilities
{
    public class QueuedHostedService : IHostedService
    {
        private readonly IServiceProvider _services;

        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly ILogger _logger;
        private Task _backgroundTask;

        public QueuedHostedService(
            IServiceProvider services,
            IBackgroundTaskQueue taskQueue,
            ILoggerFactory loggerFactory)
        {
            this._services = services;
            this.TaskQueue = taskQueue;
            this._logger = loggerFactory.CreateLogger<QueuedHostedService>();
        }

        public IBackgroundTaskQueue TaskQueue { get; }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Queued Hosted Service is starting.");

            this._backgroundTask = Task.Run(this.BackgroundProceessing);

            return Task.CompletedTask;
        }

        private async Task BackgroundProceessing()
        {
            while (!this._shutdown.IsCancellationRequested)
            {
                var workOrder = await this.TaskQueue.DequeueAsync(this._shutdown.Token);

                try
                {
                    using (var scope = this._services.CreateScope())
                    {
                        var workerType = workOrder
                            .GetType()
                            .GetInterfaces()
                            .First(t => t.IsConstructedGenericType && t.GetGenericTypeDefinition() == typeof(IBackgroundWorkOrder<,>))
                            .GetGenericArguments()
                            .Last();

                        var worker = scope.ServiceProvider
                            .GetRequiredService(workerType);

                        var task = (Task)workerType
                            .GetMethod("DoWork")
                            .Invoke(worker, new object[] { workOrder, this._shutdown.Token });
                        await task;
                    }
                }
                catch (Exception ex)
                {
                    this._logger.LogError(ex,
                        $"Error occurred executing {nameof(workOrder)}.");
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            this._logger.LogInformation("Queued Hosted Service is stopping.");

            this._shutdown.Cancel();

            return Task.WhenAny(
                this._backgroundTask,
                Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }
}
