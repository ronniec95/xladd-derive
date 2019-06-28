using System;
namespace aarcYahooFinETL.DataSource
{
    public class AarcStockQuoteDataSourceClient : AarcWebDataClient, IStockQuoteDataSourceClient
    {
        public AarcStockQuoteDataSourceClient(string baseUrl) : base(baseUrl)
        {
        }
    }
}
