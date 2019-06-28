using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    [Table("EarningsConsensus")]
    public class EarningsConsensus
    {
        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(16)]
        public string Ticker { get; set; }
        [Key]
        [Column(Order = 2)]
        public DateTime Date { get; set; }
        [Key]
        [Column(Order = 3)]
        public int Year { get; set; }

        public double EPSConsensus { get; set; }
    }
}