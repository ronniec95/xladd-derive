using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using AARC.Mesh.TCP;
using AARC.Model.Interfaces;
using AARC.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AARC.Service.Hosted
{
    /// <summary>
    /// Draft Service
    /// Service has a name
    /// Services has a number of methods
    /// </summary>
    class Program
    {
        public static async Task Main(string[] args)
        {
            var t = args.Where(a => a.StartsWith(@"service")).SelectMany(a => a.Split('=')).LastOrDefault();
            var p = args.Where(a => a.StartsWith(@"port")).SelectMany(a => a.Split('=')).LastOrDefault();
            var qService = PascalCase($"{t}_{p}");
            log4net.GlobalContext.Properties["LogFileName"] = $"{qService}Service";

            var hostBuilder = new HostBuilder()
                .UseConsoleLifetime()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // DiscoveryMonitor connects remotely? Needs Host/Port  
                    // MeshSocketServer allows remote and internal connections
                    services.AddOptions();

                    MeshServiceConfig.Server(services);
                    SocketServiceConfig.Transport(services);
                    
                    services.AddDbContext<AARC.RDS.AARCContext>
                    (options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")
                    ));

                    // Add our Market Data Repository
                    services.AddScoped<IMarketDataRepository, Repository.EF.MarketDataRepository>();
                    // Might be worth doing this as a factory with type and suppliying name for more configurability
                    services.AddSingleton<IMeshObservable<IList<string>>>(new MeshObservable<IList<string>>("nasdaqtestin"));
                    services.AddSingleton<IMeshObserver<IDictionary<string, IAarcPrice>>>(new MeshObserver<IDictionary<string, IAarcPrice>>("nasdaqtestout") );
                    services.AddSingleton<IMeshReactor<MeshMessage> ,Mesh.Dataflow.NasdaqTradableTickers>();

                    services.AddHostedService<MeshHostedService>();
                })
                .ConfigureLogging((hostContex, logging) =>
                {
                    logging.AddLog4Net();
                    logging.SetMinimumLevel(LogLevel.Debug);
                });
            await hostBuilder.RunConsoleAsync();
        }

        static string PascalCase(string str)
        {
            TextInfo cultInfo = new CultureInfo("en-US", false).TextInfo;
            str = Regex.Replace(str, "([A-Z]+)", " $1");
            str = cultInfo.ToTitleCase(str);
            str = str.Replace(" ", "");
            return str;
        }
    }
}
