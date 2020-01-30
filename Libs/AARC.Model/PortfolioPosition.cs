using System;

namespace AARC.Model
{
    public class PortfolioPosition
    {
        public string Ticker { get; set; }
        public string Type { get; set; }
        public DateTime? Expiry { get; set; }
        public double? Strike { get; set; }
        public double Price { get; set; }
        public double Quantity { get; set; }
    }

    public class pnl
    {
        public double Profit { get; set; }
        public double Percent { get; set; }
    }

    // Need a function:
    // public List<pnl> CalculateProfit(List<PortfolioPosition> positions)
}
