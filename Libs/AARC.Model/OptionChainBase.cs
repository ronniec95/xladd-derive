using System;
using System.Collections.Generic;

namespace AARC.Model
{
    public class OptionChainBase
    {
        public DateTime Expiry { get; set; }
        public List<Option> Puts { get; set; }
        public List<Option> Calls { get; set; }
    }

    public class OptionChainsBase
    {
        public string UnderlyingId { get; set; }
        public double UnderlyingPrice { get; set; }

        public DateTime Date { get; set; }                                  // The date for which this chain's prices are valid?
        public List<DateTime> Expirations { get; set; }                     // List of expiry dates
    }
}
