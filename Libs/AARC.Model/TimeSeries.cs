using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace AARC.Model
{
    // So TimeSeries - is the class that should be used which hides the implementation (e.g. list or dictionary or whatever)
    // 

    // faster than dictionary for iteration, but not if a lot of finds are required - i.e. accessing by date
    // also, an array could be better, and find could e.g. use binary search if dates are always sorted (as they should be) but not guaranteed here given add function
    public class TimeSeriesList
    {
        public TimeSeriesList()
        {
            Dates = new List<DateTime>();
            Values = new List<double>();
        }

        /// <summary>
        /// Creates a deep copy
        /// </summary>
        /// <param name="dates"></param>
        /// <param name="values"></param>
        public TimeSeriesList(IEnumerable<DateTime> dates, IEnumerable<double> values) : this()
        {
            Dates.AddRange(dates);
            Values.AddRange(values);
        }

        public List<DateTime> Dates;
        public List<double> Values;

        public int Count => Dates.Count;

        public double this[DateTime d]
        {
            get
            {
                int index = Dates.FindIndex(dateTime => dateTime >= d);
                return Values[index];
            }
        }
        public double this[int i] => Values[i];

        public void Add(DateTime date, double value)
        {
            Dates.Add(date);
            Values.Add(value);
        }
    }

    // List of times & values
    // What are the values? Well, typically just e.g. prices, but might have other information such as greeks, etc? 
    public class TimeSeries<T>
    {
        public TimeSeries()
        {
            
        }

        public TimeSeries(IList<DateTime> dates, IList<T> values)
        {
            Debug.Assert(dates.Count == values.Count);
            for (int i=0; i<dates.Count; i++)
                Data.Add(dates[i], values[i]);
        }

        public Dictionary<DateTime, T> Data = new Dictionary<DateTime,T>();

        public T this[DateTime d] => Data[d];
        public T this[int i] => Data.Values.ToList()[i];
        public IEnumerable<DateTime> Dates => Data.Keys;
        public IEnumerable<T> Values => Data.Values;

        public void Add(DateTime date, T value) { Data.Add(date, value); }
    }

    //public class MarketSeries
    //{
    //    public Underlying Underlying { get; set; }
    //    public TimeSeries<MarketElement> MarketData;
    //    //public double TicksPerDay { get; set; }
    //}

    //// is this useuful? Probably not - would prefer no underlying in it...
    //public struct MarketElement
    //{
    //    //public Underlying Underlying { get; set; }
    //    //public System.DateTime Date { get; set; }
    //    //public double TicksPerDay { get; set; }
    //    public double Close { get; set; }

    //    public Nullable<double> High { get; set; }
    //    public Nullable<double> Low { get; set; }

    //    public Nullable<double> Bid { get; set; }
    //    public Nullable<double> Mid { get; set; }
    //    public Nullable<double> Ask { get; set; }

    //    public Nullable<int> Volume { get; set; }
    //}

    /*
    public class TimeSeries
    {
        public List<TimeSeriesElement> Data;

        public TimeSeries(List<HistoricalData> list)
        {
            this.Data = new List<TimeSeriesElement>();
            foreach (HistoricalData h in list)
                this.Data.Add(new TimeSeriesElement(h));
        }

        public static implicit operator TimeSeries(List<HistoricalData> l)
        {
            return new TimeSeries(l);
        }

        public TimeSeriesElement this[int i]
        {
            get { return Data[i]; }
        }
    }

    public class TimeSeriesElement
    {
        public int idHistoricalData { get; set; }
        public string Ticker { get; set; }
        public System.DateTime CreatedDate { get; set; }
        public System.DateTime dDate { get; set; }
        public double fClose { get; set; }
        public Nullable<double> fHigh { get; set; }
        public Nullable<double> fLow { get; set; }
        public Nullable<int> iVolume { get; set; }
        public bool Intraday { get; set; }

        public TimeSeriesElement()
        {
        }

        public TimeSeriesElement(HistoricalData h)
        {
            this.idHistoricalData = h.idHistoricalData;
            this.Ticker = h.Ticker;
            this.CreatedDate = h.CreatedDate;
            this.dDate = h.dDate;
            this.fClose = h.fClose;
            this.fHigh = h.fHigh;
            this.fLow = h.fLow;
            this.iVolume = h.iVolume;
            this.Intraday = h.Intraday;
        }
    }
    */
}
