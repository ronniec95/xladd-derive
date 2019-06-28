using System;
using System.Collections.Generic;

namespace AARC.Model
{
    public class EarningsInformation
    {
        public EarningsInformation()
        {
            Initialise();
        }

        public EarningsInformation(List<DateTime> earningsDates)
        {
            EarningsDates = earningsDates;
            Initialise();
        }

        private void Initialise()
        {
            DayAfterEarningsReturns = new TimeSeriesList();
            ReturnsWithoutEarnings = new TimeSeriesList();
            ReturnsWithEarningsAsZero = new TimeSeriesList();
            FakeSeriesZeroEarnings = new TimeSeriesList();
            EarningsMultiplier = 1.0;
        }

        public List<DateTime> EarningsDates;

        public double EarningsMultiplier;

        /// <summary>
        /// The volatility of the "fake" series with NO earnings dates (not with zeros, just omitted)
        /// </summary>
        public double VolWithoutEarnings;

        /// <summary>
        /// % Returns for day after earnings
        /// </summary>
        public TimeSeriesList DayAfterEarningsReturns;

        /// <summary>
        /// % Returns not including earnings dates
        /// </summary>
        public TimeSeriesList ReturnsWithoutEarnings;

        /// <summary>
        /// % Returns for all dates, but with zeros for after earnings dates
        /// </summary>
        public TimeSeriesList ReturnsWithEarningsAsZero;

        /// <summary>
        /// A pretend series, where every earnings date had a zero return
        /// </summary>
        public TimeSeriesList FakeSeriesZeroEarnings;
    }
}
