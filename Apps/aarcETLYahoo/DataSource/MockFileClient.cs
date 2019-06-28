using System.Threading.Tasks;

namespace aarcYahooFinETL.DataSource
{
    public class MockFileClient
    {
        protected string _path;

        public MockFileClient(string path)
        {
            _path = path;
        }

        public virtual async Task<string> Request(string symbol)
        {
            using (var reader = System.IO.File.OpenText(_path))
            {
                var stringTask = reader.ReadToEndAsync();

                await stringTask;

                return stringTask.Result;
            }
        }
    }
}
