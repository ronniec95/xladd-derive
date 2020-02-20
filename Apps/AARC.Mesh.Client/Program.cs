using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using AARC.Mesh.TCP;
using AARC.Model;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

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
            var providers = new LoggerProviderCollection();

            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                        .Enrich.FromLogContext()
                        .WriteTo.Console()
                        .WriteTo.Providers(providers)
                        .CreateLogger();

            //ManualResetEvent dsConnectEvent = new ManualResetEvent(false);
            Log.Information("Starting up");
            var mc = new MeshClient(args);

            mc.Services.AddSingleton(providers);
            mc.Services.AddSingleton<ILoggerFactory>(sc =>
            {
                var providerCollection = sc.GetService<LoggerProviderCollection>();
                var factory = new SerilogLoggerFactory(null, true, providerCollection);

                foreach (var provider in sc.GetServices<ILoggerProvider>())
                    factory.AddProvider(provider);

                return factory;
            });

            mc.BuildServiceProvider();
//            mc.Services.AddLogging(l => l.AddConsole());

            var logger = mc.GetLogger<ILogger<Program>>();

            var channel = mc.Configuration.GetValue<string>("channel");

            logger.LogInformation($"Mesh Start with channel {channel}");
            var mesh = mc.Start();

            var o = mc.CreateObserver<List<string>>(channel);
            for (; ; )
            {
                var data = Console.ReadLine();
                o.OnNext(new List<string> { data });
            }

        }

        public static void Test1(MeshClient msm)
        {
            var logger = msm.GetLogger<Program>();

            logger.LogDebug("Starting subscribers to nasdaqtestout");
            try
            {

                var nasdaqTickers = msm.CreateObservable<TickerPrices>("nasdaqtestout");
                //                var nasdaqUpdater = msm.CreateObserver<List<string>>("nasdaqtestin");
                var biggeststocks = msm.CreateObservable<List<Stock>>("biggeststocks");

                nasdaqTickers.Subscribe((tickerprices) =>
                {
                    logger.LogInformation($"{tickerprices.Ticker} Updated {tickerprices.Dates.Max()}-{tickerprices.Dates.Min()}");
//                    dsConnectEvent.Set();
                });
                biggeststocks.Subscribe((stocks) =>
                {
                    logger.LogInformation($"Biggiest Stocks [{stocks.Count}]");
                    foreach (var stock in stocks)
                        logger.LogInformation($"{stock.Ticker} {stock.MarketCap}, {stock.HasOptions}");
                    //                    dsConnectEvent.Set();
                });

                Task.Delay(-1).Wait();
                //Task.Delay(30000).Wait();
/*                for (; ; )
                {
                    dsConnectEvent.Reset();
                    logger.LogInformation("Sending Ticker update");
                    var tickers = new List<string> { "AAPL" };
                    nasdaqUpdater.OnNext(tickers);
                    dsConnectEvent.WaitOne();
                }*/
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
