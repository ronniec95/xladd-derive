using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AARC.Model
{
    [Table("HolidayCalendar")]
    public class AarcHoliday
    {
        // CountryCode PK char(3) NOT NULL
        // Date PK datetime NOT NULL
        // Holiday nvarchar(50) NOT NULL

        [Key]
        [Column(Order = 1)]
        [Required]
        [StringLength(3)]
        public string CountryCode { get; set; }

        [Key]
        [Column(Order = 2)]
        public DateTime Date { get; set; }

        [Required]
        [StringLength(50)]
        public string Holiday { get; set; }
    }
}
