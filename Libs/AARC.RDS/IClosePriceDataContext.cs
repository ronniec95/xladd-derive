using Microsoft.EntityFrameworkCore;

namespace AARC.RDS
{
    using AARC.Model;
    public interface IClosePriceDataContext : IEntityDataContext
    {
        DbSet<UnderlyingPrice> UnderlyingPrices { get; set; }
    }
}
