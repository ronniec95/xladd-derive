using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    [Table("HistoricalData")]
    public class HistoricalData
    {
        [Key]
        public int idHistoricalData { get; set; }

        public string Ticker { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime dDate { get; set; }
        public double fClose { get; set; }
        public Nullable<double> fHigh { get; set; }
        public Nullable<double> fLow { get; set; }
        public Nullable<int> iVolume { get; set; }
        public bool Intraday { get; set; }
    }
}
