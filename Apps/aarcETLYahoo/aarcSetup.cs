using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using AARC.Model;
using AARC.Model.Comparers;
using AARC.Model.Interfaces.RDS;
using AARC.RDS;
using AARC.Repository.EF;
using AARC.Repository.ORM;
using aarcYahooFinETL.DataSource;
using aarcYahooFinETL.Utilities;


namespace aarcYahooFinETL
{
    public static class aarcSetup
    {
        public static void Services(IConfiguration configuration, IServiceCollection services)
        {
            services.AddRazorPages();

            services.AddDbContext<AARCContext> ((serviceProvider, options ) =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"), sqlAction => sqlAction.CommandTimeout((int)TimeSpan.FromMinutes(5).TotalSeconds) )
            );

            // Services
            services.AddScoped<BackgroundWorkItem.Worker>();
            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();

            AarcServices(services, "http://localhost:3000");

            services.AddAuthorization();
        }

        public static void AarcServices(IServiceCollection services, string yahooapi)
        {
            // Object Comparers to work with Upsert
            services.AddScoped<IEqualityComparer<AARC.Model.UnderlyingPrice>, TickerDateComparer>();
            services.AddScoped<IEqualityComparer<AARC.Model.Stock>, TickerComparer>();
            // Closing Prices
            services.AddScoped <IRepository<ServiceStats> , ServiceStatsRepository > ();
            services.AddScoped <IRepository<UnderlyingPrice>, ClosePriceDataRepository> ();

            services.AddScoped<ClosePriceDataRepository, ClosePriceDataRepository>();
            services.AddScoped<ClosingPriceOrmRepository, ClosingPriceOrmRepository>();

            // DataSources
            services.AddScoped<IClosingPricesClient>(s => new AarcClosingPricesClient($"{yahooapi}/stock/history/"));

            // Indices
            services.AddScoped<IRepository<Stock>, IndicesDataRepository>();

            // Indices DataSources
            services.AddScoped<IIndicesDataSource>(s => new AarcIndicesDataSourceClient($"{yahooapi}/stock/index/"));

            services.AddScoped<IStockQuoteDataSourceClient>(s => new AarcStockQuoteDataSourceClient($"{yahooapi}/stock/quote/"));
            // DataSources
            services.AddScoped<IStockStatsDataSourceClient>(s => new AarcStockStatsDataClient($"{yahooapi}/stock/info/"));

        }

        public static void MockServices(IServiceCollection services)
        {
            // Close Prices Repository
            services.AddScoped<IRepository<UnderlyingPrice>, ClosePriceDataRepository>();

            // Close Prices DataSources
            services.AddScoped<IClosingPricesClient>(s => new MockClosingPricesClient("/Volumes/Thunder1/Homes/ab/Projects/ClosePrices"));

            // Indices Repository
            services.AddScoped<IRepository<Stock>, IndicesDataRepository>();

            // Indices DataSources
            services.AddScoped<IIndicesDataSource>(s => new MockIndicesDataSource("/Volumes/Thunder1/Homes/ab/Projects/nasdaq20190430.json"));

            // Stocks Repository
            services.AddScoped<IRepository<StockInfo>, StockInfoRepository>();

            // Stock Quote DataSource
            services.AddScoped<IStockQuoteDataSourceClient>(s => new MockStockQuotesDataSource("/Volumes/Thunder1/Homes/ab/Projects/aaplstockquotes20190905.json"));

            // Stock Stats DataSources
            services.AddScoped<IStockStatsDataSourceClient>(s => new MockStockStatsDataSource("/Volumes/Thunder1/Homes/ab/Projects/aaplstats20190508.json"));
        }
    }
}
