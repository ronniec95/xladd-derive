using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

// ReSharper disable SuggestVarOrType_SimpleTypes
// ReSharper disable SuggestVarOrType_BuiltInTypes

// TODO: new ideas: 
// 1] api provider/consumer on cloud - e.g. https://www.mashape.com/
// 2] time & date provider / API / stock / vols / etc..
// 3] improved access to google api in an app .. leverage existing but with better interface
// 4] movie site - quizzes and new releases etc, etc


// Examples: 
// 1. When New Year's Day falls on weekend, it is not 'observed' - exchange is open - IF IT's a saturday, but if 1/1 is a sunday, 2/1 is a holiday?!?
// 2. Christmas Day is always observed
// 3. Easter - is not observed (sunday)
// 29 & 30th 2010 - exchange closed - why - Hurricane Sandy (US)!!
// NYSE closings: http://www1.nyse.com/pdfs/closings.pdf

namespace AARC.DateLib
{
    /// <summary>
    /// I didn't write this! It came from codeproject. Minor fixes and cleanups
    /// </summary>
    public class HolidayCalculator
    {
        private const string XmlPath = "Holidays.xml";
        private readonly XmlDocument _xHolidays;
        private DateTime _startingDate;

        public List<Holiday> OrderedHolidays { get; }   // is set required?

        /// <summary>
        /// Returns all of the holidays occuring in the year following the date that is passed in the constructor.  Holidays are defined in an XML file.
        /// </summary>	
        /// <param name="startDate">The starting date for returning holidays.  All holidays for one year after this date are returned.</param>
        [Obsolete("Don't use this - it has not been fixed", true)]
        public HolidayCalculator(DateTime startDate)
        {
            _startingDate = startDate;

            //if (!System.IO.File.Exists(xmlPath))
            //{
            //    // TODO: Why does this differ from @"E:\Dev\AARC\Data\Holdays.xml" ???
            //    xmlPath = "E:\\Dev\\AARC\\Data\\Holidays.xml";
            //    if (!System.IO.File.Exists(xmlPath))
            //        throw new System.IO.FileNotFoundException("Could not find file", xmlPath);
            //}

            _xHolidays = new XmlDocument();
            _xHolidays.Load(XmlPath);

            OrderedHolidays = ProcessXml();
        }

        public HolidayCalculator(DateTime startDate, DateTime endDate)
        {
            _startingDate = startDate;

            _xHolidays = new XmlDocument();
            _xHolidays.Load(XmlPath);

            // need to:
            // 1. subtract some days from the start; and
            // 2. add some days to the end
            // BECAUSE ... otherwise this might miss an "observed" holiday that falls outside the period
            DateTime firstDate = startDate.AddDays(-10);
            DateTime lastDate = endDate.AddDays(10);

            List<Holiday> holidays = new List<Holiday>();
            for (_startingDate = firstDate; _startingDate <= lastDate; _startingDate = _startingDate.AddYears(1))
                holidays.AddRange(ProcessXml());

            OrderedHolidays = holidays.Where(x => x.Date >= startDate && x.Date <= endDate).Distinct().OrderBy(x => x.Date).ToList();
        }

        #region Private Methods

        /// <summary>
        /// Loops through the holidays defined in the XML configuration file, and adds the next occurance into the OrderHolidays collection if it occurs within one year.
        /// </summary>
        private List<Holiday> ProcessXml()
        {
            List<Holiday> holidays = new List<Holiday>();

            XmlNodeList nodeList = _xHolidays.SelectNodes("/Holidays/Holiday");
            if (nodeList == null)
                return holidays;

            // LINQ #1
            //List<XmlNode> nodes = (from XmlNode n in nodeList select n).ToList();
            //holidays = nodes.Select(x => ProcessNode(x)).Where(x => x.Date.Year > 1).ToList();

            // LINQ #2
            //holidays.AddRange(from XmlNode n in nodeList select ProcessNode(n) into h where h.Date.Year > 1 select h);

            // Foreach loop
            foreach (XmlNode n in nodeList)
            {
                Holiday h = ProcessNode(n);
                if (h.Date.Year > 1)
                {
                    holidays.Add(h);

                    if (h.Date.IsWeekend() && h.Observed > 0)
                    {
                        // TODO: Is this the correct handling? is a saturday holiday sometimes observed on a monday? is a sunday ever observed on a friday?
                        // add an 'obvserved' holiday - for holidays that fall on weekends
                        Holiday hObserved = new Holiday {Name = h.Name + " (observed)", Date = h.Date, Observed = h.Observed};

                        // Special handling...
                        int daysAdded = 0;
                        if (h.Name == "Christmas Day")
                        {
                            if (hObserved.Date.DayOfWeek == DayOfWeek.Saturday)
                                // Saturday - make it Friday
                                hObserved.Date = h.Date.AddDays(-1);
                            else
                                // Sunday - make it Monday
                                hObserved.Date = h.Date.AddDays(1);
                        }
                        else
                        {
                            while (hObserved.Date.IsWeekend())
                            {
                                hObserved.Date = hObserved.Date.AddDays(1);
                                daysAdded++;
                            }
                        }

                        if (daysAdded <= hObserved.Observed)
                            holidays.Add(hObserved);
                    }
                }
                else
                {
                    Console.WriteLine("What is happening here??");
                }
            }

            holidays.Sort();

            return holidays;
        }

        /*
        private Holiday ProcessNodeNew(XmlNode n)
        {
            if (n == null || n.Attributes == null)
                return null;

            Holiday h = new Holiday { Name = n.Attributes["name"].Value };
            Console.WriteLine(h.Name);

            foreach (XmlNode o in n.ChildNodes)
            {
            }
        }*/

        /*
		<Day>1</Day>
		<Month>1</Month>

        <DayOfWeek>1</DayOfWeek>
		<WeekOfMonth>3</WeekOfMonth>
		
        <EveryXYears>4</EveryXYears>
		<StartYear>1940</StartYear>

        <DaysAfterHoliday Holiday="Easter">
			<Days>-2</Days>
		</DaysAfterHoliday>

		<DayOfWeekOnOrAfter>
			<DayOfWeek>2</DayOfWeek>
			<Month>11</Month>
			<Day>2</Day>
		</DayOfWeekOnOrAfter>
        
		<WeekdayOnOrAfter>
			<Month>4</Month>
			<Day>15</Day>
		</WeekdayOnOrAfter>

        Methods:
        /// Gets the next occurance of a weekday after a given month and day in the year after startDate.
        GetDateByWeekdayOnOrAfter 

        /// Gets the n'th instance of a day-of-week in the given month after StartDate
        GetDateByMonthWeekWeekday

            WeekOfMonth
                int m = int.Parse(n.SelectSingleNode("./Month").InnerXml);
                int w = int.Parse(n.SelectSingleNode("./WeekOfMonth").InnerXml);
                int wd = int.Parse(n.SelectSingleNode("./DayOfWeek").InnerXml);
                GetDateByMonthWeekWeekday

            DayOfWeekOnOrAfter
                int dow = int.Parse(n.SelectSingleNode("./DayOfWeekOnOrAfter/DayOfWeek").InnerXml);
                if (dow > 6 || dow < 0)
                    throw new Exception("DOW is greater than 6");
                int m = int.Parse(n.SelectSingleNode("./DayOfWeekOnOrAfter/Month").InnerXml);
                int d = int.Parse(n.SelectSingleNode("./DayOfWeekOnOrAfter/Day").InnerXml);
                h.Date = GetDateByWeekdayOnOrAfter(dow, m, d, _startingDate);

            WeekdayOnOrAfter
                int m = int.Parse(n.SelectSingleNode("./WeekdayOnOrAfter/Month").InnerXml);
                int d = int.Parse(n.SelectSingleNode("./WeekdayOnOrAfter/Day").InnerXml);
                DateTime dt = new DateTime(_startingDate.Year, m, d);
                if (dt < _startingDate)
                    dt = dt.AddYears(1);
                while (dt.DayOfWeek.Equals(DayOfWeek.Saturday) || dt.DayOfWeek.Equals(DayOfWeek.Sunday))
                    dt = dt.AddDays(1);
                h.Date = dt;

            
            LastFullWeekOfMonth
                int m = int.Parse(n.SelectSingleNode("./LastFullWeekOfMonth/Month").InnerXml);
                int weekday = int.Parse(n.SelectSingleNode("./LastFullWeekOfMonth/DayOfWeek").InnerXml);
                DateTime dt = GetDateByMonthWeekWeekday(m, 5, weekday, _startingDate);

                h.Date = dt.AddDays(6 - weekday).Month == m ? dt : dt.AddDays(-7);

            DaysAfterHoliday
                XmlNode basis = _xHolidays.SelectSingleNode("/Holidays/Holiday[@name='" + n.SelectSingleNode("./DaysAfterHoliday").Attributes["Holiday"].Value + "']");
                Holiday bHoliday = ProcessNode(basis);
                int days = int.Parse(n.SelectSingleNode("./DaysAfterHoliday/Days").InnerXml);
                h.Date = bHoliday.Date.AddDays(days);

            Easter

            Month && Day

            // TODO: Utilise Observed and remove recursion, and fix _start/_end date stuff
            Observed - up to how many days after the holiday the date may be observed
        */


        /// <summary>
        /// Processes a Holiday node from the XML configuration file.
        /// </summary>
        /// <param name="n">The Holdiay node to process.</param>
        /// <returns></returns>
        private Holiday ProcessNode(XmlNode n)
        {
            if (n == null || n.Attributes == null)
                return null;

            Holiday h = new Holiday {Name = n.Attributes["name"].Value};

            //Console.WriteLine(h.Name);

            ArrayList childNodes = new ArrayList();
            foreach (XmlNode o in n.ChildNodes)
            {
                childNodes.Add(o.Name);
            }

            if (childNodes.Contains("WeekOfMonth"))
            {
                int m = int.Parse(n.SelectSingleNode("./Month").InnerXml);
                int w = int.Parse(n.SelectSingleNode("./WeekOfMonth").InnerXml);
                int wd = int.Parse(n.SelectSingleNode("./DayOfWeek").InnerXml);
                h.Date = GetDateByMonthWeekWeekday(m, w, wd, _startingDate);
            }
            else if (childNodes.Contains("DayOfWeekOnOrAfter"))
            {
                int dow = int.Parse(n.SelectSingleNode("./DayOfWeekOnOrAfter/DayOfWeek").InnerXml);
                if (dow > 6 || dow < 0)
                    throw new Exception("DOW is greater than 6");
                int m = int.Parse(n.SelectSingleNode("./DayOfWeekOnOrAfter/Month").InnerXml);
                int d = int.Parse(n.SelectSingleNode("./DayOfWeekOnOrAfter/Day").InnerXml);
                h.Date = GetDateByWeekdayOnOrAfter(dow, m, d, _startingDate);
            }
            else if (childNodes.Contains("WeekdayOnOrAfter"))
            {
                int m = int.Parse(n.SelectSingleNode("./WeekdayOnOrAfter/Month").InnerXml);
                int d = int.Parse(n.SelectSingleNode("./WeekdayOnOrAfter/Day").InnerXml);
                DateTime dt = new DateTime(_startingDate.Year, m, d);
                if (dt < _startingDate)
                    dt = dt.AddYears(1);
                while (dt.DayOfWeek.Equals(DayOfWeek.Saturday) || dt.DayOfWeek.Equals(DayOfWeek.Sunday))
                    dt = dt.AddDays(1);
                h.Date = dt;
            }
            else if (childNodes.Contains("LastFullWeekOfMonth"))
            {
                int m = int.Parse(n.SelectSingleNode("./LastFullWeekOfMonth/Month").InnerXml);
                int weekday = int.Parse(n.SelectSingleNode("./LastFullWeekOfMonth/DayOfWeek").InnerXml);
                DateTime dt = GetDateByMonthWeekWeekday(m, 5, weekday, _startingDate);

                h.Date = dt.AddDays(6 - weekday).Month == m ? dt : dt.AddDays(-7);
            }
            else if (childNodes.Contains("DaysAfterHoliday"))
            {
                XmlNode basis = _xHolidays.SelectSingleNode("/Holidays/Holiday[@name='" + n.SelectSingleNode("./DaysAfterHoliday").Attributes["Holiday"].Value + "']");
                Holiday bHoliday = ProcessNode(basis);
                int days = int.Parse(n.SelectSingleNode("./DaysAfterHoliday/Days").InnerXml);
                h.Date = bHoliday.Date.AddDays(days);
            }
            else if (childNodes.Contains("Easter"))
            {
                h.Date = Easter(_startingDate);
            }
            else
            {
                if (childNodes.Contains("Month") && childNodes.Contains("Day"))
                {
                    int m = int.Parse(n.SelectSingleNode("./Month").InnerXml);
                    int d = int.Parse(n.SelectSingleNode("./Day").InnerXml);
                    DateTime dt = new DateTime(_startingDate.Year, m, d);
                    if (dt < _startingDate)
                    {
                        dt = dt.AddYears(1);
                    }
                    if (childNodes.Contains("EveryXYears"))
                    {
                        int yearMult = int.Parse(n.SelectSingleNode("./EveryXYears").InnerXml);
                        int startYear = int.Parse(n.SelectSingleNode("./StartYear").InnerXml);
                        if (((dt.Year - startYear) % yearMult) == 0)
                        {
                            h.Date = dt;
                        }
                    }
                    else
                    {
                        h.Date = dt;
                    }
                }
            }

            if (childNodes.Contains("Observed"))
            {
                h.Observed = int.Parse(n.SelectSingleNode("Observed").InnerXml);
            }

            return h;
        }


        /// <summary>
        /// Determines the next occurance of Easter (western Christian).
        /// </summary>
        /// <returns></returns>
        private static DateTime Easter(DateTime startDate)
        {
            DateTime workDate = GetFirstDayOfMonth(startDate);
            int y = workDate.Year;
            if (workDate.Month > 4)
                y = y + 1;

            return Easter(y, startDate);
        }


        /// <summary>
        /// Determines the occurance of Easter in the year following startDate.
        /// If the result comes before StartDate, recalculates for the following year.
        /// </summary>
        /// <param name="y"></param>
        /// <param name="startDate">the date after which to find the day sought</param>
        /// <returns></returns>
        private static DateTime Easter(int y, DateTime startDate)
        {
            while (true)
            {
                int a = y%19;
                int b = y/100;
                int c = y%100;
                int d = b/4;
                int e = b%4;
                int f = (b + 8)/25;
                int g = (b - f + 1)/3;
                int h = (19*a + b - d - g + 15)%30;
                int i = c/4;
                int k = c%4;
                int l = (32 + 2*e + 2*i - h - k)%7;
                int m = (a + 11*h + 22*l)/451;
                int easterMonth = (h + l - 7*m + 114)/31;
                int p = (h + l - 7*m + 114)%31;
                int easterDay = p + 1;
                DateTime est = new DateTime(y, easterMonth, easterDay);
                if (est >= startDate)
                    return new DateTime(y, easterMonth, easterDay);

                y = y + 1;
            }
        }

        /// <summary>
        /// Gets the next occurance of a weekday after a given month and day in the year after startDate.
        /// </summary>
        /// <param name="weekday">The day of the week (0=Sunday)</param>
        /// <param name="m">The Month</param>
        /// <param name="d">Day</param>
        /// <param name="startDate">the date after which to find the day sought</param>
        /// <returns></returns>
        private DateTime GetDateByWeekdayOnOrAfter(int weekday, int m, int d, DateTime startDate)
        {
            while (true)
            {
                DateTime workDate = GetFirstDayOfMonth(startDate);
                while (workDate.Month != m)
                {
                    workDate = workDate.AddMonths(1);
                }
                workDate = workDate.AddDays(d - 1);

                while (weekday != (int) (workDate.DayOfWeek))
                {
                    workDate = workDate.AddDays(1);
                }

                //It's possible the resulting date is before the specified starting date.  If so we'll calculate again for the next year.
                if (workDate < _startingDate)
                {
                    startDate = startDate.AddYears(1);
                    continue;
                }

                return workDate;
            }

            /*
                DateTime workDate = GetFirstDayOfMonth(startDate);
                while (workDate.Month != m)
                {
                    workDate = workDate.AddMonths(1);
                }
                workDate = workDate.AddDays(d - 1);

                while (weekday != (int)(workDate.DayOfWeek))
                {
                    workDate = workDate.AddDays(1);
                }

                //It's possible the resulting date is before the specified starting date.  If so we'll calculate again for the next year.
                if (workDate < _startingDate)
                    return GetDateByWeekdayOnOrAfter(weekday, m, d, startDate.AddYears(1));
                return workDate;
            */
        }

        /// <summary>
        /// Gets the n'th instance of a day-of-week in the given month after StartDate
        /// </summary>
        /// <param name="month">The month the Holiday falls on.</param>
        /// <param name="week">The instance of weekday that the Holiday falls on (5=last instance in the month).</param>
        /// <param name="weekday">The day of the week that the Holiday falls on.</param>
        /// <param name="startDate">the date after which to find the day sought</param>
        /// <returns></returns>
        private DateTime GetDateByMonthWeekWeekday(int month, int week, int weekday, DateTime startDate)
        {
            while (true)
            {
                DateTime workDate = GetFirstDayOfMonth(startDate);
                while (workDate.Month != month)
                    workDate = workDate.AddMonths(1);

                while ((int) workDate.DayOfWeek != weekday)
                    workDate = workDate.AddDays(1);

                DateTime result;
                if (week == 1)
                {
                    result = workDate;
                }
                else
                {
                    int addDays = (week*7) - 7;
                    int day = workDate.Day + addDays;
                    if (day > DateTime.DaysInMonth(workDate.Year, workDate.Month))
                    {
                        day = day - 7;
                    }
                    result = new DateTime(workDate.Year, workDate.Month, day);
                }

                //It's possible the resulting date is before the specified starting date.  If so we'll calculate again for the next year.
                if (result >= _startingDate)
                    return result;

                startDate = startDate.AddYears(1);
            }

            /*
            DateTime workDate = GetFirstDayOfMonth(startDate);
            while (workDate.Month != month)
            {
                workDate = workDate.AddMonths(1);
            }
            while ((int)workDate.DayOfWeek != weekday)
            {
                workDate = workDate.AddDays(1);
            }

            DateTime result;
            if (week == 1)
            {
                result = workDate;
            }
            else
            {
                int addDays = (week * 7) - 7;
                int day = workDate.Day + addDays;
                if (day > DateTime.DaysInMonth(workDate.Year, workDate.Month))
                {
                    day = day - 7;
                }
                result = new DateTime(workDate.Year, workDate.Month, day);
            }

            //It's possible the resulting date is before the specified starting date.  If so we'll calculate again for the next year.
            if (result >= _startingDate)
                return result;
            return GetDateByMonthWeekWeekday(month, week, weekday, startDate.AddYears(1));
            */
        }

        /// <summary>
        /// Returns the first day of the month for the specified date.
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private static DateTime GetFirstDayOfMonth(DateTime dt)
        {
            return new DateTime(dt.Year, dt.Month, 1);
        }
        #endregion

        #region Holiday Object
        public class Holiday : IComparable
        {
            public int Observed = 2;
            public DateTime Date;
            public string Name;

            #region IComparable Members

            public int CompareTo(object obj)
            {
                Holiday h = obj as Holiday;
                if (h == null)
                    throw new ArgumentException("Object is not a Holiday");

                return Date.CompareTo(h.Date);
            }
            #endregion
        }
        #endregion
    }
}
