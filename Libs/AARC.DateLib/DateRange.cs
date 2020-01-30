using System;
using System.Collections.Generic;

namespace AARC.DateLib
{
    public class DateRange
    {
        public DateRange(Tuple<DateTime, DateTime> twoDates)
        {
            Start = twoDates.Item1;
            End = twoDates.Item2;
        }

        public DateRange(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        public DateTime Start;
        public DateTime End;

        // ReSharper disable InconsistentNaming
        public enum DateRangePeriod { All, Flat_00_13, P1_Falling_00_03, P2_Rising_03_07, P3_Crash_07_07, P4_Rising_09_15 };
        // ReSharper enable InconsistentNaming

        public static Dictionary<DateRangePeriod, DateRange> MarketPeriods => _periods ?? (_periods = GetPeriods());

        private static Dictionary<DateRangePeriod, DateRange> _periods;

        private static Dictionary<DateRangePeriod, DateRange> GetPeriods()
        {
            // Some interesting/useful date periods
            DateTime fStart = new DateTime(2000, 7, 31);     // SP=1517
            DateTime fEnd = new DateTime(2013, 2, 1);      // SP=1514

            // interesting dates: 31/Jul/00 down to 1/Feb/03 up to 30/Sep/07 down to 1/Feb/09 up to 30/4/15...
            DateTime t1 = new DateTime(2000, 7, 31);
            DateTime t2 = new DateTime(2003, 2, 1);
            DateTime t3 = new DateTime(2007, 9, 30);
            DateTime t4 = new DateTime(2009, 2, 1);
            DateTime t5 = new DateTime(2015, 4, 30);

            Dictionary<DateRangePeriod, DateRange> periodDictionary = new Dictionary<DateRangePeriod, DateRange>
            {
                { DateRangePeriod.All, new DateRange(t1, t5) },
                { DateRangePeriod.Flat_00_13, new DateRange(fStart, fEnd) },
                { DateRangePeriod.P1_Falling_00_03, new DateRange(t1, t2) },
                { DateRangePeriod.P2_Rising_03_07, new DateRange(t2, t3) },
                { DateRangePeriod.P3_Crash_07_07, new DateRange(t3, t4) },
                { DateRangePeriod.P4_Rising_09_15, new DateRange(t4, t5) }
            };

            return periodDictionary;
        }
    }
}
