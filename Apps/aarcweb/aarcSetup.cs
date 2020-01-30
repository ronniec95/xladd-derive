using AARC.Repository.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace aarcweb
{
    public static class aarcSetup
    {
        public static void Services(IConfiguration configuration, IServiceCollection services)
        {
            services.AddDbContext<AARC.RDS.AARCContext>
            (options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")
            ));

            // Add our Market Data Repository
            services.AddScoped<IMarketDataRepository, AARC.Repository.EF.MarketDataRepository>();

            // Add our Stock Data Provider
            services.AddScoped<IStockRepository, AARC.Repository.EF.StockRepository>();
        }
    }
}
