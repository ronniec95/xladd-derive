using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace AARC.DateLib
{
    public static class DateHelperExtensions
    {
        public static bool IsWorkingDay(this DateTime date)
        {
            return (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday);
        }

        public static bool IsWeekend(this DateTime date)
        {
            return (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday);
        }
        /// <summary>
        /// Gives all the mon-fri dates between the range.
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public static IEnumerable<DateTime> WorkDayRange(this DateTime startDate, DateTime endDate)
        {
            return Enumerable
                .Range(0, (endDate - startDate).Days + 1)
                .Select(d => startDate.AddDays(d))
                .Where(d => d.DayOfWeek != DayOfWeek.Saturday && d.DayOfWeek != DayOfWeek.Sunday);
        }
    }

    /// <summary>
    /// Handles business logic to do with Dates, utilises the DataLayer where required
    /// </summary>
    public class DateHelper
    {
        public static DateTime TryParse(string dateString, DateTime defaultValue)
        {
            string[] formats = { "d/M/yyyy", "dd/MM/yyyy", "d/MM/yyyy", "dd/M/yyyy" };
                         //"MM/dd/yyyy hh:mm:ss", "M/d/yyyy h:mm:ss",
                         //"M/d/yyyy hh:mm tt", "M/d/yyyy hh tt",
                         //"M/d/yyyy h:mm", "M/d/yyyy h:mm",
                         //"MM/dd/yyyy hh:mm", "M/dd/yyyy hh:mm"};
            DateTime dateTime;
            if (!DateTime.TryParseExact(dateString, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out dateTime))
                dateTime = defaultValue;

            return dateTime;
        }

        public static List<DateTime> GetTradingDays(DateTime startDate, DateTime endDate)
        {
            List<DateTime> tradingDays = new List<DateTime>();
            for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
                if (!d.IsWeekend() && !Holidays.IsHoliday(d))
                    tradingDays.Add(d);

            return tradingDays;
        }

        public static List<DateTime> GetWeekends(DateTime startDate, DateTime endDate)
        {
            List<DateTime> weekends = new List<DateTime>();
            for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
                if (d.IsWeekend())
                    weekends.Add(d);

            return weekends;
        }

        public static List<DateTime> GetHolidays(DateTime startDate, DateTime endDate)
        {
            List<DateTime> holidays = new List<DateTime>();
            for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
                if (Holidays.IsHoliday(d))
                    holidays.Add(d);

            return holidays;
        }


        public static void TestDateHelper()
        {
            DateTime d = AddTradingDays(new DateTime(2001, 1, 12), 2);  // 12 friday, 13 sat, 14 sun, 15 mlk holiday, 16=1, 17=2
            Debug.Assert(d.Year == 2001 && d.Month == 1 && d.Day == 17);

            d = AddTradingDays(new DateTime(2001, 1, 17), -2);          // back to friday 12...
            Debug.Assert(d.Year == 2001 && d.Month == 1 && d.Day == 12);

            d = GetNextTradingDay(new DateTime(2001, 1, 12));
            Debug.Assert(d.Year == 2001 && d.Month == 1 && d.Day == 16);

            d = GetPreviousTradingDay(new DateTime(2001, 2, 20));
            Debug.Assert(d.Year == 2001 && d.Month == 2 && d.Day == 16);

            int n = GetNumTradingDays(new DateTime(2001, 1, 12), new DateTime(2001, 1, 15));
            Debug.Assert(n == 0);

            n = GetNumTradingDays(new DateTime(2001, 1, 12), new DateTime(2001, 1, 17));
            Debug.Assert(n == 2);

            n = GetNumTradingDays(new DateTime(2001, 1, 1), new DateTime(2002, 1, 1));
            int n2 = BusinessDaysUntil(new DateTime(2001, 1, 1), new DateTime(2002, 1, 1));
            Debug.Assert(n == n2);
        }

        public static List<DateTime> FixExpiries(IEnumerable<DateTime> uniqueExpiryDates)
        {
            // This occurs often: if the expiry date is a SATURDAY then need to get the previous trading day...
            return uniqueExpiryDates.Select(date => date.DayOfWeek == DayOfWeek.Saturday ? GetPreviousTradingDay(date) : date).ToList();
        }

        public static DateTime FixExpiry(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday ? GetPreviousTradingDay(date) : date;
        }

        // TODO: Refactor all the trading days & dates code.. properly... utilising holidays

        // but ultimately if this is the place for it...then it needs to access the holiday calendar, and probably needs a ticker too, as ultimately it will depend on where
        public static DateTime AddTradingDays(DateTime d, int number)
        {
            int numAdded = 0;
            DateTime result = d;
            while (numAdded++ < Math.Abs(number))
                result = number > 0 ? GetNextTradingDay(result) : GetPreviousTradingDay(result);
            return result;
        }

        public static DateTime GetPreviousTradingDay(DateTime d)
        {
            DateTime next = d.AddDays(-1);
            while (!IsTradingDay(next))
                next = next.AddDays(-1);
            return next;
        }

        public static DateTime GetNextTradingDay(DateTime d)
        {
            DateTime next = d.AddDays(1);
            while (!IsTradingDay(next))
                next = next.AddDays(1);
            return next;
        }

        public static bool IsTradingDay(DateTime date)
        {
            return date.IsWorkingDay() && !Holidays.IsHoliday(date);
        }

        public static int[] GetNumTradingDays(DateTime start, IList<DateTime> dates)
        {
            return dates.Select(x => GetNumTradingDays(start, x)).ToArray();
        }

        /// <summary>
        /// Allows end to be before start in which case returns -ve number
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static int GetNumTradingDaysEither(DateTime start, DateTime end)
        {
            if (end > start)
                return GetNumTradingDays(start, end);

            var totalDays = 0;
            for (DateTime date = start; date > end; date = AddTradingDays(date, -1))
            {
                totalDays--;
            }

            return totalDays;
        }

        public static int GetNumTradingDays(DateTime start, DateTime end)
        {
            var totalDays = 0;
            for (DateTime date = start; date < end; date = AddTradingDays(date, 1))
            {
                //if (IsTradingDay(date))
                    totalDays++;
            }

            return totalDays;
        }

        // Yet another shady example of Date handling code
        public static List<DateTime> GetTradingDays(DateTime startDate, DateTime endDate, IEnumerable<DateTime> holidays)
        {
            DateTime firstDate = startDate.Date;
            DateTime lastDate = endDate.Date;

            // LINQ
            //List<DateTime> allDays = Enumerable.Range(0, 1 + lastDate.Subtract(firstDate).Days).Select(offset => firstDate.AddDays(offset)).ToArray();

            // For loop
            List<DateTime> allDays = new List<DateTime>();
            for (var dt = firstDate; dt <= lastDate; dt = dt.AddDays(1))
                allDays.Add(dt);

            // While loop
            //while (firstDate < lastDate)
            //{
            //    allDays.Add(firstDate);
            //    firstDate = firstDate.AddDays(1);
            //}

            List<DateTime> tradingDays = allDays.Where(x => x.IsWorkingDay() && (holidays == null || !holidays.Contains(x))).ToList();

            return tradingDays;
        }

        public static int BusinessDaysUntil(DateTime firstDay, DateTime lastDay)
        {
            return BusinessDaysUntil(firstDay, lastDay, Holidays.GetHolidays(firstDay, lastDay));
        }

        // More efficient implementation??
        // http://stackoverflow.com/questions/1617049/calculate-the-number-of-business-days-between-two-dates
        // TODO: is it correct?
        public static int BusinessDaysUntil(DateTime firstDay, DateTime lastDay, IEnumerable<DateTime> bankHolidays)
        {
            firstDay = firstDay.Date;
            lastDay = lastDay.Date;
            if (firstDay > lastDay)
                throw new ArgumentException("BusinessDaysUntil: Last day must come after first day");

            TimeSpan span = lastDay - firstDay;
            int businessDays = span.Days + 1;
            int fullWeekCount = businessDays / 7;
            // find out if there are weekends during the time exceedng the full weeks
            if (businessDays > fullWeekCount * 7)
            {
                // we are here to find out if there is a 1-day or 2-days weekend
                // in the time interval remaining after subtracting the complete weeks

                //int firstDayOfWeek = (int)firstDay.DayOfWeek;
                //int lastDayOfWeek = (int)lastDay.DayOfWeek;
                // Bugfix from the original page
                int firstDayOfWeek = firstDay.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)firstDay.DayOfWeek;
                int lastDayOfWeek = lastDay.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)lastDay.DayOfWeek;

                if (lastDayOfWeek < firstDayOfWeek)
                    lastDayOfWeek += 7;

                if (firstDayOfWeek <= 6)
                {
                    if (lastDayOfWeek >= 7)// Both Saturday and Sunday are in the remaining time interval
                        businessDays -= 2;
                    else if (lastDayOfWeek >= 6)// Only Saturday is in the remaining time interval
                        businessDays -= 1;
                }
                else if (firstDayOfWeek <= 7 && lastDayOfWeek >= 7) // Only Sunday is in the remaining time interval
                {
                    businessDays -= 1;
                }
            }

            // subtract the weekends during the full weeks in the interval
            businessDays -= fullWeekCount + fullWeekCount;

            // subtract the number of bank holidays during the time interval
            foreach (DateTime bankHoliday in bankHolidays)
            {
                DateTime bh = bankHoliday.Date;
                if (firstDay <= bh && bh <= lastDay)
                    --businessDays;
            }

            return businessDays;
        }

        public static DateTime yyyyMMddToDate(long ldate)
        {
            int year = (int)(ldate / 10000);
            int month = (int)(ldate - year * 10000)/100;
            int day = (int)(ldate - year * 10000 - month * 100);

            return new DateTime(year, month, day);
        }
        // TICKER specific
        //[Obsolete("Prefer to use GetNumTradingDays")]
        //public static int GetNumTradingDays(string ticker, DateTime from, DateTime to)
        //{
        //    int numDays = AarcAdaptor.GetUnderlyingPrices(ticker, from, to).Select(x => x.Date).Distinct().Count();

        //    return numDays;
        //}

        //[Obsolete("Prefer to use AddTradingDays")]
        //public static DateTime AddTradingDays(string ticker, DateTime d, int days)
        //{
        //    DateTime result;

        //    if (days > 0)
        //    {
        //        // add 'days' trading days

        //        // EntityFunctions.TruncateTime ??
        //        // System.Data.Entity.DbFunctions.TruncateTime(mydate)

        //        List<UnderlyingPrice> underlyingPrices = AarcAdaptor.GetUnderlyingPrices(ticker, d, DateTime.MaxValue);

        //        int numDays = underlyingPrices.Count();
        //        if (numDays < days)
        //        {
        //            // TODO: Except, what if there are not enough prices?
        //            throw new NotImplementedException("how to handle this?");
        //        }

        //        result = underlyingPrices.OrderBy(x => x.Date).Select(y => y.Date).Skip(days).First();
        //    }
        //    else
        //    {
        //        List<UnderlyingPrice> underlyingPrices = AarcAdaptor.GetUnderlyingPrices(ticker, DateTime.MinValue, d);

        //        // subtracts 'days' trading days
        //        int numDays = underlyingPrices.Count();
        //        if (numDays < days)
        //        {
        //            // TODO: Except, what if there are not enough prices?
        //            throw new NotImplementedException("how to handle this?");
        //        }

        //        result = underlyingPrices.OrderByDescending(x => x.Date).Select(x => x.Date).Skip(Math.Abs(days)).First();
        //    }

        //    return result;
        //}
    }
}
