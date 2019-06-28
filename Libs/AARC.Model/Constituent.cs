using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    [Table("Constituents")]
    public class Constituent
    {
        public Constituent()
        {
            LastUpdated = DateTime.Now;
        }

        [Key, Column(Order = 0)] public string IndexName { get; set; }
        [Key, Column(Order = 1)] public string Ticker { get; set; }
        public bool Active { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
