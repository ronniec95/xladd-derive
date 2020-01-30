using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    [Table("MomentumResults2")]
    public class MomentumResult2
    {
        [Key, Column(Order = 0)] public DateTime MarketDate { get; set; }
        [Key, Column(Order = 1)] public string Ticker { get; set; }
        [Key, Column(Order = 2)] public int Strategy { get; set; }
        public double Rank { get; set; }     // simon rank
        public double Coeff { get; set; }    // slope y = ax + b = a
        public double Rsqrd { get; set; }
        public double RsqrdAdj { get; set; }
        public double PG1 { get; set; }     // PG = price gain OR loss
        public double PG2 { get; set; }
        public double PG3 { get; set; }
    }

    [Table("MomentumResults")]
    public class MomentumResult
    {
        [Key, Column(Order = 0)] public DateTime MarketDate { get; set; }
        [Key, Column(Order = 1)] public string Ticker { get; set; }
        public double Rank { get; set; }     // simon rank
        public double Coeff { get; set; }    // slope y = ax + b = a
        public double Rsqrd { get; set; }
        public int LRPeriod { get; set; }
        public double PG7 { get; set; }     // PG = price gain OR loss
        public double PG14 { get; set; }
        public double PG30 { get; set; }
        public double MPG7 { get; set; }     // PG = price gain OR loss
        public double MPG14 { get; set; }
        public double MPG30 { get; set; }
        public double MPL7 { get; set; }     // PG = price gain OR loss
        public double MPL14 { get; set; }
        public double MPL30 { get; set; }
    }
}
