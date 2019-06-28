namespace AARC.RDS
{
    using AARC.Model;
    using Microsoft.EntityFrameworkCore;
    public interface IStocksContext : IEntityDataContext
    {
        DbSet<Stock> Stocks { get; set; }
    }
}
