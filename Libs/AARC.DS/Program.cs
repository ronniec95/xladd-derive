using System;
using System.Threading.Tasks;
using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using AARC.Mesh.TCP;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AARC.DS
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            await CreateHostedDiscoveryService(args);
        }

        static async Task CreateHostedDiscoveryService(string[] args)
        {
            log4net.GlobalContext.Properties["LogFileName"] = $"DiscoveryService";
            var hostBuilder = new HostBuilder()
                .UseConsoleLifetime()
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.AddSingleton<DiscoveryMonitor<DiscoveryMessage>>();
                    services.AddSingleton<MeshSocketServer<MeshMessage>>();
                    services.AddSingleton<SocketServiceFactory>();
                    services.AddSingleton<MeshServiceManager>();
                    services.AddHostedService<DSHostedService>();
                })
                .ConfigureLogging((hostContex, configlogging) =>
                {
                    configlogging.AddDebug();
                    configlogging.AddLog4Net();
                });
            await hostBuilder.RunConsoleAsync();
        }
    }
}
