using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using AARC.Mesh.TCP;
using AARC.Model.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.Client
{
    /// <summary>
    /// Draft Service
    /// Service has a name
    /// Services has a number of methods
    /// </summary>
    class Program
    {
        public static void Main(string[] args)
        {
            log4net.GlobalContext.Properties["LogFileName"] = $"MeshTestClient";
            var cancellationTokenSrc = new CancellationTokenSource();
            var cancellationToken = cancellationTokenSrc.Token;
            var services = new ServiceCollection()
                .AddLogging()
                .AddOptions();

            MeshServiceConfig.Server(services);
            SocketServiceConfig.Transport(services);

            var serviceProvider = services.BuildServiceProvider();

            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            serviceProvider.GetService<ILoggerFactory>()
                .AddLog4Net();

            var logger = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            logger.LogDebug("Starting application");
            var msm = serviceProvider.GetService<MeshServiceManager>();

            var discoveryAddress = new Uri(config.GetValue<string>("ds", "tcp://localhost:9999"));

            // Todo: Bit of a hack as DS should supply port
            msm.ListeningPort = config.GetValue<Int32>("port", 0);

            var nasdaqTickers = new MeshObservable<IDictionary<string, IAarcPrice>>("nasdaqtestout");
            msm.RegisterChannels(nasdaqTickers);

            var nasdaqUpdater = new MeshObserver<IList<string>>("nasdaqtestin");
            msm.RegisterChannels(nasdaqUpdater);

            var t1 = msm.StartDiscoveryServices(discoveryAddress.ToString(), cancellationToken);
            // Listen for subscibers for output Qs
            var t2 = msm.StartListeningServices(cancellationToken);
            // Connect to publishers of the data we want
            var t3 = msm.StartPublisherConnections(cancellationToken);

            nasdaqTickers.Subscribe((tickerUniverse) =>
            {
                foreach (var kv in tickerUniverse)
                {
                    logger.LogInformation($"{kv.Key} update");
                }
            });

            Task.Delay(30000).Wait();
            for(var i = 0; i < 10; i ++)
            {
                Task.Delay(1000).Wait();
                logger.LogInformation("Ticker update");
                var tickers = new List<string> { "AAPL" };
                nasdaqUpdater.OnNext(tickers);
            }
            logger.LogInformation("Waiting for death");
            Task.WaitAll(t1, t2, t3);

            logger.LogDebug("All done!");
        }
    }
}
