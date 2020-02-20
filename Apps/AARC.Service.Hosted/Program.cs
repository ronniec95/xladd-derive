using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AARC.Service.Hosted
{
    using AARC.Mesh.AutoWireUp;
    using AARC.Mesh.Dataflow;
    using AARC.Mesh.Interface;
    using AARC.Mesh.SubService;
    using AARC.Mesh.TCP;
    using AARC.Repository.Interfaces;
    using Serilog;
    using Serilog.Events;
    using Serilog.Extensions.Logging;

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
                    var providers = new LoggerProviderCollection();

                    Log.Logger = new LoggerConfiguration()
                                .MinimumLevel.Debug()
                                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                                .Enrich.FromLogContext()
                                .WriteTo.Console()
                                .WriteTo.Providers(providers)
                                .CreateLogger();

                    var service = hostContext.Configuration.GetValue<string>("service", "MeshDataFlow");
                    var port = hostContext.Configuration.GetValue<string>("port", "");

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
                    services.AddScoped<IStockRepository, Repository.EF.StockRepository>();

                    // Like a bitter way of creating ms from a factory etc.
                    // services.AddSingleton<IMeshReactor<MeshMessage>, Mesh.Dataflow.BiggestStocksReactor>();
                    // services.AddScoped<IMeshNodeFactory, DataFlowFactory>();
                    // services.AddScoped<IMeshNodeFactory, AutoWireUpFactory>();
                    services.AddScoped<IMeshNodeFactory, DataFlowFactory>();
                    services.AddHostedService<MeshHostedService>();

                    services.AddSingleton(providers);
                    services.AddSingleton<ILoggerFactory>(sc =>
                    {
                        var providerCollection = sc.GetService<LoggerProviderCollection>();
                        var factory = new SerilogLoggerFactory(null, true, providerCollection);

                        foreach (var provider in sc.GetServices<ILoggerProvider>())
                            factory.AddProvider(provider);

                        return factory;
                    });
                })
                .UseSerilog();
/*                .ConfigureLogging((hostContext, logging) =>
                {
                    logging.SetMinimumLevel(LogLevel.Debug);
                });*/

    }
}
