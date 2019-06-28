namespace AARC.RDS
{
    using AARC.Model;
    using Microsoft.EntityFrameworkCore;
    public interface IMarketDataContext : IEntityDataContext
    {
        DbSet<UnderlyingPrice> UnderlyingPrices { get; set; }

        DbSet<Stock> Stocks { get; set; }
    }
}
