namespace AARC.RDS
{
    using Microsoft.EntityFrameworkCore;
    public interface IServiceStatsContext : IEntityDataContext
    {
        DbSet<AARC.Model.ServiceStats> ServiceStats { get; set; }
    }
}
