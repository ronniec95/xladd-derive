using Microsoft.EntityFrameworkCore;

namespace AARC.RDS
{
    using AARC.Model;
    public class AARCContext : DbContext, IClosePriceDataContext, IStocksDataContext, IServiceStatsContext, IMarketDataContext, IStocksContext
    {
        public AARCContext(DbContextOptions<AARCContext> options) : base(options)
        {
        }

        public DbSet<UnderlyingPrice> UnderlyingPrices { get; set; }

        public DbSet<Stock> Stocks { get; set; }

        public DbSet<ServiceStats> ServiceStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
        }
    }
}
