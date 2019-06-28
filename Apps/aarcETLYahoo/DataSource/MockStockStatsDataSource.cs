using System;
namespace aarcYahooFinETL.DataSource
{
    public class MockStockStatsDataSource : MockFileClient, IStockStatsDataSourceClient
    {
        public MockStockStatsDataSource(string path) : base(path)
        {
        }
    }
}
