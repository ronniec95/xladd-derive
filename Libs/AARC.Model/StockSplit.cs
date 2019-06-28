using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    [Table("StockSplit")]
    public class StockSplit
    {
        [Key]
        public int StockSplitId { get; set; }
        public string Ticker { get; set; }
        public DateTime Date { get; set; }
        public string Ratio { get; set; }
    }
}