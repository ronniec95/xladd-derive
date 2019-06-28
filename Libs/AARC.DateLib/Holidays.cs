using System;
using System.Collections.Generic;

namespace AARC.DateLib
{
    /**
     Some holiday options...
     * http://www.codeproject.com/Articles/11666/Dynamic-Holiday-Date-Calculator
     * http://jachman.wordpress.com/2007/11/02/public-holiday-toolkit-v10/
     * 
        // http://www.timeanddate.com/services/api/holiday-api.html
        // http://stackoverflow.com/questions/1617049/calculate-the-number-of-business-days-between-two-dates

        // NOTE: I thought the public-holiday-toolkit-v10/ was a good option, but eventually preferred the HolidayCalculator, so I could easily modify the xml
     */

    public interface IHolidays
    {
        bool IsHoliday(DateTime date);
        List<DateTime> GetAarcHolidays(DateTime from, DateTime to);

        List<DateTime> GetHolidays(DateTime from, DateTime to);
    }

    /*
    public class DBHolidays : IHolidays
    {
        private  List<DateTime> _holidays = null;
        private  DateTime _firsttDate;
        private  DateTime _lastDate;
        DBHolidays()
        {
            _firsttDate = new DateTime(2001, 1, 1);
            _lastDate = new DateTime(2016, 12, 31);

            Initialise(_firsttDate, _lastDate);
        }
        public List<DateTime> GetAarcHolidays(DateTime from, DateTime to)
        {
            return AarcAdaptor.GetHolidays(from, to).Select(x => x.Date).ToList();
        }

        public List<DateTime> GetHolidays(DateTime start, DateTime end)
        {
            Initialise(start, end);

            return _holidays.Where(x => x >= start && x <= end).OrderBy(x => x.Date).ToList();
        }

        public bool IsHoliday(DateTime date)
        {
            Initialise(date, date);

            return _holidays.Contains(date);
        }

        private void Initialise(DateTime start, DateTime end)
        {
            if (_holidays == null || start < _firsttDate || end > _lastDate)
            {
                // Adding/subtracting years here, to avoid a situation whereby we creep forward 1 day at a time... and repeatedly call this
                if (start < _firsttDate)
                    _firsttDate = start.AddYears(-1);

                if (end > _lastDate)
                    _lastDate = end.AddYears(1);

                // snp, nasdaq, holiday calc... add together to form one holiday list...
                List<DateTime> businessDays = DateHelper.GetTradingDays(_firsttDate, _lastDate, null);

                // businessDates where there is no nasdaq price are presumably nasdaq holidays
                List<DateTime> nasdaqDates = AarcAdaptor.GetUnderlyingPrices("NASDAQ", _firsttDate, _lastDate).Select(x => x.Date).ToList();
                Debug.Assert(nasdaqDates.Count > 0);    // TODO: This is a problem if entity framework connection is not working?
                DateTime minDate = nasdaqDates.Min();
                DateTime maxDate = nasdaqDates.Max();
                List<DateTime> nasdaqHolidays = businessDays.Where(x => x >= minDate && x <= maxDate && !nasdaqDates.Contains(x)).ToList();

                // businessDates where there is no sp500 price are presumably sp500 holidays
                List<DateTime> sp500Dates = AarcAdaptor.GetUnderlyingPrices(Constants.MarketTicker, _firsttDate, _lastDate).Select(x => x.Date).ToList();
                minDate = sp500Dates.Min();
                maxDate = sp500Dates.Max();
                List<DateTime> spHolidays = businessDays.Where(x => x >= minDate && x <= maxDate && !sp500Dates.Contains(x)).ToList();

                // use the holiday calculator to calculate additional holidays (i.e. missing sp500/nasdaq dates either side of the data)
                HolidayCalculator hc = new HolidayCalculator(_firsttDate, _lastDate);
                List<DateTime> hcDates = hc.OrderedHolidays.Select(x => x.Date).ToList();
                List<DateTime> hcHolidays = businessDays.Where(x => hcDates.Contains(x)).ToList();

                _holidays = new List<DateTime>();
                _holidays.AddRange(nasdaqHolidays.Union(spHolidays.Union(hcHolidays)));
            }
        }
    }*/
    public static class Holidays
    {
        private static IHolidays _holidays = null;

        public static List<DateTime> GetAarcHolidays(DateTime from, DateTime to)
        {
            return _holidays.GetAarcHolidays(from, to);
        }

        public static bool IsHoliday(DateTime date)
        {
            return _holidays.IsHoliday(date);
        }

        public static List<DateTime> GetHolidays(DateTime start, DateTime end)
        {
            return _holidays.GetHolidays(start, end);
        }

        #region Implementation

        static Holidays()
        {
        }


        #endregion Implementation
    }
}