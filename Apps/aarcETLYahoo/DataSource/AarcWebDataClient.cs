using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace aarcYahooFinETL.DataSource
{
    public interface IAarcWebDataClient
    {
        Task<string> Request(string source);
    }

    public abstract class AarcWebDataClient : IAarcWebDataClient
    {
        protected Uri _baseUri;

        public AarcWebDataClient(string baseUrl)
        {
            _baseUri = new Uri(baseUrl);
        }


        public virtual async Task<string> Request(string value)
        {
            var client = new HttpClient();

            var uri = new Uri(_baseUri, value);

            return await client.GetStringAsync(uri);
        }
    }
}
