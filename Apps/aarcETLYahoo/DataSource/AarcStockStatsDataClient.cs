using System;
namespace aarcYahooFinETL.DataSource
{
    public class AarcStockStatsDataClient : AarcWebDataClient, IStockStatsDataSourceClient
    {
        public AarcStockStatsDataClient(string baseUrl) : base(baseUrl)
        {
        }
    }
}
