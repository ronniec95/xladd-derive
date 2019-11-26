using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using AARC.Mesh.TCP;
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
                    config.AddJsonFile("appsettings.json", optional: true);
                    config.AddEnvironmentVariables();

                    if (args != null)
                        config.AddCommandLine(args);
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddOptions();
                    services.AddSingleton<ServiceHostNameFactory>();
                    services.AddSingleton<DiscoveryServiceStateMachine<MeshMessage>>();
                    services.AddSingleton<DiscoveryMonitor<DiscoveryMessage>>();
                    services.AddSingleton<IMeshTransport<MeshMessage>, MeshSocketServer<MeshMessage>>();
                    services.AddSingleton<SocketServiceFactory>();
                    services.AddSingleton<MeshServiceManager>();
                    services.AddHostedService<MeshHostedService>();
                })
                .ConfigureLogging((hostContex, configlogging) =>
                {
                    //configlogging.AddDebug();
                    configlogging.AddLog4Net();
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
