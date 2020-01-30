using Microsoft.EntityFrameworkCore;

namespace AARC.RDS
{
    public interface IStocksDataContext : IEntityDataContext
    {
        DbSet<AARC.Model.Stock> Stocks { get; set; }
    }
}
