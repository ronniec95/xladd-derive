using System;
namespace aarcYahooFinETL.DataSource
{
    public class MockStockQuotesDataSource : MockFileClient, IStockQuoteDataSourceClient
    {
        public MockStockQuotesDataSource(string path) : base(path)
        {
        }
    }
}
