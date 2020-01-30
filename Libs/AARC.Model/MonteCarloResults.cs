using System.Collections.Generic;

namespace AARC.Model
{
    public class MonteCarloResults
    {
        public List<double[]> Prices { get; set; }
        public List<double[]> Vols { get; set; }
        public List<double> Earnings { get; set; }
    }
}