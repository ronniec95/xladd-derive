using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    /*
        Ex/Eff Date	
        Type	
        Cash Amount	
        Declaration Date	
        Record Date	
        Payment Date
    */

    // also put in other things like splits here?
    public enum DividendType { Dividend = 0, Split = 1 }

    [Table("Dividends")]
    public class Dividend
    {
        public Dividend()
        {
            LastUpdated = DateTime.Now;
        }

        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(16)]
        public string Ticker { get; set; }

        [Key]
        [Column(Order = 2)]
        [Required]
        public DateTime ExDate { get; set; }    // exdate or split date

        public int Type { get; set; }

        public double Amount { get; set; }  // dividend amount or split

        public DateTime? DeclarationDate { get; set; }
        public DateTime? RecordDate { get; set; }
        public DateTime? PaymentDate { get; set; }

        public DateTime LastUpdated { get; set; }
    }

}
