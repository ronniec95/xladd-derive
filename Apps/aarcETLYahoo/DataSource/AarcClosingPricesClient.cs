using System;
using System.IO;
using System.Threading.Tasks;
using AARC.Utilities;

namespace aarcYahooFinETL.DataSource
{
    public class AarcClosingPricesClient : AarcWebDataClient, IClosingPricesClient
    {
        public AarcClosingPricesClient(string baseUrl) : base(baseUrl)
        {
        }

        public override Task<string> Request(string value)
        {
            var r = base.Request(value);
#if DEBUG
            var message = r.Result;
            var path = Path.Combine(@"../../ClosingPrices", value);
            if (!Directory.Exists(path))
            {
                var di = Directory.CreateDirectory(path);
            }

            var symbol = PathHelper.GetHeadPath(value);

            var filename = Path.Combine(path, $"{symbol}_{DateTime.Now:yyyyMMddHHmmss}.json");
            File.WriteAllText(filename, message);
#endif
            return r;
        }
    }
}
