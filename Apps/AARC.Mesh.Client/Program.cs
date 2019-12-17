using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using AARC.Mesh.TCP;
using AARC.Model;
using System.Linq;

namespace AARC.Mesh.Client
{
    /// <summary>
    /// Draft Service
    /// Service has a name
    /// Services has a number of methods
    /// </summary>
    class Program
    {


        class MeshClient : IDisposable
        {
            MeshServiceManager msm;
            static CancellationTokenSource _cts = new CancellationTokenSource();
            public IServiceProvider ServiceProvider;
            public Task DiscoveryService;
            public Task ChannelSubscriber;
            public Task ChannelPublisher;
            public MeshClient(string[] args)
            {
                var cancellationToken = _cts.Token;

                var services = new ServiceCollection()
                    .AddLogging()
                    .AddOptions();

                MeshServiceConfig.Server(services);
                SocketServiceConfig.Transport(services);

                ServiceProvider = services.BuildServiceProvider();

                var config = new ConfigurationBuilder()
                    .AddCommandLine(args)
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                ServiceProvider.GetService<ILoggerFactory>()
                    .AddLog4Net();

                msm = ServiceProvider.GetService<MeshServiceManager>();

                var discoveryAddress = new Uri(config.GetValue<string>("ds", "tcp://localhost:9999"));

                // Todo: Bit of a hack as DS should supply port
                msm.ListeningPort = config.GetValue<Int32>("port", 0);

                DiscoveryService = msm.StartDiscoveryServices(discoveryAddress.ToString(), cancellationToken);
                // Listen for subscibers for output Qs
                ChannelSubscriber = msm.StartListeningServices(cancellationToken);
                // Connect to publishers of the data we want
                ChannelPublisher = msm.StartPublisherConnections(cancellationToken);
            }
            public IObservable<T> CreateObservable<T>(string channel) where T: class,new()
            {
                var observable = new MeshObservable<T>(channel);
                msm.RegisterChannels(observable);
                return observable;
            }

            public IObserver<T> CreateObserver<T>(string channel) where T: class,new()
            {
                var observer = new MeshObserver<T>(channel);
                msm.RegisterChannels(observer);
                return observer;
            }

            public void Stop()
            {
                if (!_cts.IsCancellationRequested)
                    _cts.Cancel();
                Task.WaitAll(DiscoveryService, ChannelPublisher, ChannelSubscriber);
                DiscoveryService = Task.CompletedTask;
                ChannelPublisher = Task.CompletedTask;
                ChannelSubscriber = Task.CompletedTask;
            }
            public void Dispose() => Stop();
        }
        public static void Main(string[] args)
        {
            ManualResetEvent dsConnectEvent = new ManualResetEvent(false);
            log4net.GlobalContext.Properties["LogFileName"] = $"MeshTestClient";

            var msm = new MeshClient(args);

            var logger = msm.ServiceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            logger.LogDebug("Starting application");
            try
            {
                var nasdaqTickers = msm.CreateObservable<TickerPrices>("nasdaqtestout");
                var nasdaqUpdater = msm.CreateObserver<List<string>>("nasdaqtestin");

                nasdaqTickers.Subscribe((tickerprices) =>
                {
                    logger.LogInformation($"{tickerprices.Ticker} Updated {tickerprices.Dates.Max()}-{tickerprices.Dates.Min()}");
                    dsConnectEvent.Set();
                });

                Task.Delay(30000).Wait();
                for (; ; )
                {
                    dsConnectEvent.Reset();
                    logger.LogInformation("Sending Ticker update");
                    var tickers = new List<string> { "AAPL" };
                    nasdaqUpdater.OnNext(tickers);
                    dsConnectEvent.WaitOne();
                }
            }
            finally
            {
                msm.Stop();
                logger.LogInformation("Waiting for death");
            }
            logger.LogDebug("All done!");
        }
    }
}
