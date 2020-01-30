using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace aarcYahooFinETL.DataSource
{
    public class MockClosingPricesClient : MockFileClient, IClosingPricesClient
    {
        public MockClosingPricesClient(string path): base(path)
        {

        }

        public override async Task<string> Request(string symbol)
        {
            var directory = new DirectoryInfo(_path);
                
            var files = directory.GetFiles()
                .Where(f => f.Name.Contains($"_{symbol.ToUpper()}_"))
                .OrderByDescending(f => f.LastWriteTime);

            var file = files.Select(f => f.FullName).FirstOrDefault();

            using (var reader = System.IO.File.OpenText(file))
            {
                var stringTask = reader.ReadToEndAsync();

                await stringTask;

                return stringTask.Result;
            }
        }
    }
}
