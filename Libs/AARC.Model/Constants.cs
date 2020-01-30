namespace AARC.Model
{
    public static class Constants
    {
        public const double Epsilon = 1e-10;
        public const double MaxPermittedVol = 2.99;

        public const string MarketTicker = "SP500";   // TODO: NEEDS to be: SPX for options in TBSP.  ^GSPC -- used to be SP500 in DB - change now??
        public const string NasdaqTicker = "NDX";   // Nasdaq100 but close enough? Yahoo: ^NDX. Also Nasdaq Composite - Yahoo: ^IXIC, Google: .IXIC
        public const string VixTicker = "VIX";      // ^VIX
    }
}
