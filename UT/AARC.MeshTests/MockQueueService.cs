using System;
using AARC.Mesh.Model;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using AARC.Model;

namespace AARC.MeshTests
{
    class MockQueueService : SubscriberPattern<byte[]>, IMeshServiceTransport
    {
        public ConcurrentQueue<byte[]> messagesin = new ConcurrentQueue<byte[]>();
        public MockQueueService(string url)
        {
            Url = url;
        }
        public bool Connected => throw new NotImplementedException();


        public string Url { get; private set; }

        public bool ConnectionAlive()
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        public void OnPublish(byte[] value)
        {
            messagesin.Enqueue(value);
            var m = new MeshMessage();
            m.Decode(value);
            var tickers = JsonConvert.DeserializeObject<List<string>>(m.PayLoad);
            var tickerUniverse = new Dictionary<string, TickerPrices>();
            foreach (var t in tickers)
                tickerUniverse[t] = new TickerPrices { Ticker = t };

            var opayload = JsonConvert.SerializeObject(tickerUniverse);
            var o = new MeshMessage { Service = Url, Channel = "nasdaqtestout", PayLoad = opayload };
            var obytes = o.Encode();
            foreach (var p in _publishers)
                p.OnPublish(obytes);
        }

        public void ReadAsync()
        {
            throw new NotImplementedException();
        }

        public void Shutdown()
        {
            throw new NotImplementedException();
        }
    }

}
