using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
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
        public static void Main(string[] args)
        {
            //ManualResetEvent dsConnectEvent = new ManualResetEvent(false);
            log4net.GlobalContext.Properties["LogFileName"] = $"MeshTestClient";
            var msm = new MeshClient(args);

            var logger = msm.ServiceProvider.GetService<ILoggerFactory>()
    .CreateLogger<Program>();

            var o = msm.CreateObserver<string>(args[0]);
            for (; ; )
            {
                var data = Console.ReadLine();
                o.OnNext(data);
            }
        }

        public static void Test1(MeshClient msm)
        {
            var logger = msm.ServiceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

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
