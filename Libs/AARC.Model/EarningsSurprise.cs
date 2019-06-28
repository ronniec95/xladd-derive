using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    [Table("EarningsSurprise")]
    public class EarningsSurprise
    {
        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(16)]
        public string Ticker { get; set; }

        [Key]
        [Column(Order = 2)]
        public DateTime Date { get; set; }

        public double EPS { get; set; }
        public double Surprise { get; set; }
    }
}
