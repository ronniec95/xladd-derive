using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    [Table("EarningsDatesThree")]
    public class EarningsDateThree
    {
        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(16)]
        public string Ticker { get; set; }
        // NOTE: Ticker could instead (also) be a foreign key to Stocks table - e.g. [ForeignKey("Stocks")]

        [Key]
        [Column(Order = 2)]
        [Required]
        public DateTime Date { get; set; }

        public bool AfterClose { get; set; }            // if not after close, then before open

        [StringLength(30)]
        public string Source { get; set; }              // where this date came from

        public double EstimatedEps { get; set; }
        public double ReportedEps { get; set; }

        public double EstimatedRevenue { get; set; }
        public double ReportedRevenue { get; set; }

        // can use [NotMapped] for class/property/field declaractions
        // can also use partial class to extend...??
        public string ToNiceString()
        {
            string ac = AfterClose ? "AfterClose" : "BeforeOpen";
            return $"{Ticker} {Date:ddd dd-MMM-yyyy HH:mm:ss} {ac} {Source} EstEps={EstimatedEps:F2} ReportedEps={ReportedEps:F2} EstRevenue={EstimatedRevenue} ReportedRevenue={ReportedRevenue}";
        }

        public EarningsDateThree Clone()
        {
            EarningsDateThree clone = new EarningsDateThree();
            clone.Ticker = Ticker;
            clone.Date = Date;
            clone.AfterClose = AfterClose;
            clone.Source = Source;
            clone.EstimatedEps = EstimatedEps;
            clone.ReportedEps = ReportedEps;
            clone.EstimatedRevenue = EstimatedRevenue;
            clone.ReportedRevenue = ReportedRevenue;
            return clone;
        }
    }

    [Table("EarningsDates")]
    public class EarningsDate
    {
    //[Ticker] [nvarchar](50) NOT NULL,
    //[Date] [datetime] NOT NULL,
    //[AfterClose] [bit] NOT NULL,
    //[Source] [nvarchar](50) NOT NULL,
    //[Verified] [bit] NOT NULL,
    //[AvgIVBE] [float] NOT NULL,
    //[AvgIVAE] [float] NOT NULL,
    //[PctUnderlyingChange] [float] NOT NULL,

        // https://msdn.microsoft.com/en-us/data/jj591583
        // column order specified for composite primary key

        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(50)]
        public string Ticker { get; set; }
        // NOTE: Ticker could instead (also) be a foreign key to Stocks table - e.g. [ForeignKey("Stocks")]

        [Key]
        [Column(Order = 2)]
        public DateTime Date { get; set; }

        public bool AfterClose { get; set; }            // if not after close, then before open

        [Required]
        [StringLength(50)]
        public string Source { get; set; }              // where this date came from

        public bool Verified { get; set; }              // if this data was verified from some source (e.g. optionslam.com) - or e.g. estimated

        public double AvgIVBE { get; set; }             // avg implied vol before earnings (end of day)
        public double AvgIVAE { get; set; }             // avg implied vol after earnings (end of day)

        public double PctUnderlyingChange { get; set; }           // pct change in underlying price (end of day)

        // what else would be nice? analysts estimates
        // %change since last earnings (, vs mkt)
    }


    [Table("EarningsDatesExt")]
    public class EarningsDateExtended
    {
        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(20)]
        public string Ticker { get; set; }

        [Key]
        [Column(Order = 2)]
        public DateTime Date { get; set; }

        public byte DayOfWeek { get; set; }

        public DateTime Before { get; set; }
        public DateTime After { get; set; }

        public bool AfterClose { get; set; }            // if not after close, then before open

        [Required]
        [StringLength(50)]
        public string Source { get; set; }              // where this date came from

        public bool Verified { get; set; }              // if this data was verified from some source (e.g. optionslam.com) - or e.g. estimated

        public double AvgIVBE { get; set; }             // avg implied vol before earnings (end of day)
        public double AvgIVAE { get; set; }             // avg implied vol after earnings (end of day)

        public double PctUnderlyingChange { get; set; }           // pct change in underlying price (end of day)
    }
}
