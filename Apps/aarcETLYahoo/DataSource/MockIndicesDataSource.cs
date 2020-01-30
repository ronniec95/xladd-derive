using System;
using System.Threading.Tasks;

namespace aarcYahooFinETL.DataSource
{
    public class MockIndicesDataSource : MockFileClient, IIndicesDataSource
    {
        public MockIndicesDataSource(string path) : base(path) 
        {
        }
    }
}
