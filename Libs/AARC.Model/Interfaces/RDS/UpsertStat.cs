using System;
namespace AARC.Model.Interfaces.RDS
{
    public class UpsertStat
    {
        public int NewRows { get; set; }
        public int UpdatedRows { get; set; }
        public int UnchangedRows { get; set; }
        public int TotalRows { get; set; }
    }
}
