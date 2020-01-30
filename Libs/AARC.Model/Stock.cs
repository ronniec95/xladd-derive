using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AARC.Model.Interfaces;

namespace AARC.Model
{
    [Table("Stocks")]
    public class Stock : IAarcInstrument
    {
        public Stock()
        {
            LastUpdated = DateTime.Now;
        }

        public Stock(string ticker)
        {
            Ticker = ticker;
            LastUpdated = DateTime.Now;
        }
        [Key]
        public string Ticker { get; set; }
        public string Description { get; set; }
        public string Exchange { get; set; }

        public bool? HasOptions { get; set; }
        public double? MarketCap { get; set; }
        public double? Dividend { get; set; }

        public DateTime LastUpdated { get; set; }

        public byte? OptionCategory { get; set; }

        // New since Jun 17
        public string Sector { get; set; }
        public string Industry { get; set; }
        // ReSharper disable once InconsistentNaming
        public DateTime? IPO { get; set; }

        [NotMapped]
        public string LocalSymbol => Ticker;

        [NotMapped]
        public AarcContractType ContractType => AarcContractType.Stock;
    }

}
