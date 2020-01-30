namespace AARC.Model
{
    using System;
    using System.ComponentModel.DataAnnotations;
    public class ServiceStats : AARC.Model.Interfaces.RDS.UpsertStat
    {
        [Key]
        public string Instance { get; set; }
        public DateTime Start { get; set; }
        public DateTime Updated { get; set; }
        public string Service { get; set; }
        public string Status { get; set; }
        public string Symbol { get; set; }
        public string Message { get; set; }
    }
}
