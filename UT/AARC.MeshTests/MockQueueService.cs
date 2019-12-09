using System;
using AARC.Mesh.Model;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using AARC.Model;

namespace AARC.MeshTests
{
    class MockQueueService : SubscriberPattern<byte[]>, IMeshChannelService
    {
        public ConcurrentQueue<byte[]> messagesin = new ConcurrentQueue<byte[]>();
        public MockQueueService(string transportId)
        {
            ServiceDetails = transportId;
        }
        public bool Connected => throw new NotImplementedException();


        public string ServiceDetails { get; private set; }

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
            var o = new MeshMessage { Service = ServiceDetails, QueueName = "nasdaqtestout", PayLoad = opayload };
            var obytes = o.Encode();
            foreach (var p in _publishers)
                p.OnPublish(obytes);
        }

        public void ReadAsync()
        {
            throw new NotImplementedException();
        }
    }

}
