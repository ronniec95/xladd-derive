using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;
using AARC.Model.Interfaces;

namespace AARC.Model
{
    // Unset is there to be used to ensure type is set - i.e. it should never be unset - the default value
    public enum OptionType { Unset = 0, Put = 1, Call = 2, Both = 3 };

    public static class OptionHelper
    {
        public static OptionType ParseType(string s)
        {
            string lower = s.ToLower();
            return (lower == "c" || lower == "call") ? OptionType.Call : OptionType.Put;
        }
    }

    public class OptionEqualityComparer : IEqualityComparer<Option>
    {
        public bool Equals(Option x, Option y)
        {
            return x.ContractEquals(y);
        }

        public int GetHashCode(Option obj)
        {
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// AARC Option class
    /// </summary>
    public class Option : AarcOptionBase, IAarcInstrument
    {
        public static string GetName(string ticker, DateTime expiry, double strike, OptionType type)
        {
            return $"{ticker}_{expiry:ddMMyyyy}_{strike}{(type == OptionType.Call ? "C" : "P")}";
        }

        //private string _name;
        public string Name => GetName(Ticker, Expiry, Strike, OType);
        //{
        //    // NOTE: G will output decimal places if present... as will no format specifier... 
        //    get 
        //    {
        //        //if (_name == null)
        //        //    _name = $"{Ticker}_{Expiry:ddMMyyyy}_{Strike}{(OType == OptionType.Call ? "C" : "P")}";
        //        //return _name;

        //        return $"{Ticker}_{Expiry:ddMMyyyy}_{Strike}{(OType == OptionType.Call ? "C" : "P")}";
        //    }
        //}

        // TODO: Hardcodes...
        //const double BidAskSpread = 0.015;  // 1.5% each way = 3% from Bid to Ask
        private const double BidAskSpread = 0.02;  // 2% each way = 4% from Bid to Ask
        private const double BidPercent = 1 - BidAskSpread;
        private const double AskPercent = 1 + BidAskSpread;

        public void SetValue(Option o)
        {
            Bid = o.Bid;
            Ask = o.Ask;
            Mid = o.Mid;
            Close = o.Close;
        }

        public void SetValue(double midPrice)
        {
            Mid = midPrice;
            Bid = Math.Round(midPrice * BidPercent, 3);
            Ask = Math.Round(midPrice * AskPercent, 3);
            Close = midPrice;
        }

        public Option Clone()
        {
            return new Option(this);
        }

        [Browsable(false)]
        public int OptionId { get; set; }

        [Browsable(false)]
        public MarketSource Source { get; set; }

        public DateTime Date { get; set; }

        public string Ticker { get; set; }

        public double Strike { get; set; }

        [DisplayName("Type")]
        public OptionType OType { get; set; }

        [NotMapped]
        public string LocalSymbol { get { return Name; } }
        [NotMapped]
        [DisplayName("Contract")]
        public AarcContractType ContractType { get { return AarcContractType.Option; } }
        [NotMapped]
        public string PutCall
        {
            get { return OType == OptionType.Call ? "Call" : "Put"; }
            set { OType = value.Equals("Call") ? OptionType.Call : OptionType.Put; }
        }

        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Mid { get; set; }
        public double Close { get; set; }

        public double ImpliedVol { get; set; }

        public double Volume { get; set; }
        public double OpenInterest { get; set; }
        public double UnderlyingPrice { get; set; }
        public DateTime Expiry { get; set; }

        public Option()
        {
            Source = MarketSource.UnSet;
        }

        public Option(MarketSource source)
        {
            Source = source;
        }

        public Option(Option o)
        {
            OptionId = o.OptionId;
            Date = o.Date;
            Source = o.Source;
            Ticker = o.Ticker;
            Strike = o.Strike;
            OType = o.OType;
            Expiry = o.Expiry;

            Bid = o.Bid;
            Ask = o.Ask;
            Mid = o.Mid;
            Close = o.Close;

            OpenInterest = o.OpenInterest;
            UnderlyingPrice = o.UnderlyingPrice;
            Volume = o.Volume;
            ImpliedVol = o.ImpliedVol;
        }

        public bool ContractEquals(Option o)
        {
            return Strike.Equals(o.Strike) && Expiry == o.Expiry && OType == o.OType;
        }

        public static bool ContractsAreEqual(Option o1, Option o2)
        {
            return o1.Strike.Equals(o2.Strike) && o1.Expiry == o2.Expiry && o1.OType == o2.OType;
        }
    }
}
