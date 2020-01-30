using AARC.Mesh.TCP;
using AARC.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace AARCMeshClient
{
    class Program
    {
        /// <summary>
        /// Simple example of a publisher IObserver and a subscriber IObservable
        /// </summary>
        /// <param name="args">List of tickers</param>
        static void Main(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                Console.WriteLine("Please set tickers on the command line");
                return;
            }
            ManualResetEvent dsConnectEvent = new ManualResetEvent(false);
            log4net.GlobalContext.Properties["LogFileName"] = $"MeshTestClient";

            var msm = new MeshClient(args);

            Console.WriteLine("Starting application");
            try
            {
                var nasdaqTickers = msm.CreateObservable<TickerPrices>("nasdaqtestout");

                nasdaqTickers.Subscribe((tickerprices) =>
                {
                    Console.WriteLine($"{tickerprices.Ticker} Updated {tickerprices.Dates.Max()}-{tickerprices.Dates.Min()}");
                });

                // Todo: Mesh needs to tell us its up and connected.
                Task.Delay(30000).Wait();
                var nasdaqUpdater = msm.CreateObserver<List<string>>("nasdaqtestin");

                while (true)
                {
                    Console.WriteLine($"Sending Ticker updates {string.Join(",", args)}");
                    // Todo: We need to know we have a route/connection
                    nasdaqUpdater.OnNext(args.ToList());
                    Task.Delay(30000).Wait();
                }

            }
            finally
            {
                msm.Stop();
                Console.WriteLine("Waiting for death");
            }
            Console.WriteLine("All done!");
        }
    }
}
