using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace aarcYahooFinETL.DataSource
{
    public class AarcIndicesDataSourceClient : AarcWebDataClient, IIndicesDataSource
    {
        public AarcIndicesDataSourceClient(string baseUrl) : base(baseUrl)
        {
        }
    }
}
