using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AARC.Service.Hosted
{
    using AARC.Mesh.Interface;
    using AARC.Mesh.Model;
    using AARC.Mesh.SubService;
    using AARC.Mesh.TCP;
    using AARC.Model.Interfaces;
    using AARC.Repository.Interfaces;
    /// <summary>
    /// Draft Service
    /// Service has a name
    /// Services has a number of methods
    /// </summary>
    class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    var service = hostContext.Configuration.GetValue<string>("service", "MeshDataFlow");
                    var port = hostContext.Configuration.GetValue<string>("port", "");
                    log4net.GlobalContext.Properties["LogFileName"] = $"{service}_{port}";

                    // DiscoveryMonitor connects remotely? Needs Host/Port  
                    // MeshSocketServer allows remote and internal connections
                    services.AddOptions();

                    MeshServiceConfig.Server(services);
                    SocketServiceConfig.Transport(services);

                    services.AddDbContext<RDS.AARCContext>
                    (options =>
                        options.UseSqlServer(hostContext.Configuration.GetConnectionString("DefaultConnection")
                    ));

                    // Add our Market Data Repository
                    services.AddScoped<IMarketDataRepository, Repository.EF.MarketDataRepository>();
                    // Might be worth doing this as a factory with type and suppliying name for more configurability
                    //                    services.AddSingleton<IMeshReactor<MeshMessage>, Mesh.Dataflow.NasdaqTradableTickers>();

                    services.AddScoped<IStockRepository, Repository.EF.StockRepository>();
                    services.AddSingleton<IMeshReactor<MeshMessage>, Mesh.Dataflow.BiggestStocksReactor>();

                    services.AddHostedService<MeshHostedService>();
                })
                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                    logging.AddLog4Net();
                });

    }
}
