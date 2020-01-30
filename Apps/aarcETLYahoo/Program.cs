using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace aarcYahooFinETL
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
               .ConfigureLogging((hostingContext, logging) =>
               {
                   // The ILoggingBuilder minimum level determines the
                   // the lowest possible level for logging. The log4net
                   // level then sets the level that we actually log at.
                   logging.AddLog4Net();
                   logging.SetMinimumLevel(LogLevel.Debug);
               });
    }
}
