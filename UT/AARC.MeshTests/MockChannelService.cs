using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Newtonsoft.Json;


namespace AARC.MeshTests
{
    using AARC.Mesh.Model;
    using AARC.Model;
    using AARC.Mesh.Interface;
    using AARC.Mesh;
    using System.Threading.Channels;

    class MockChannelService : SubscriberPattern<byte[]>, IMeshServiceTransport
    {
        public ConcurrentQueue<byte[]> messagesin = new ConcurrentQueue<byte[]>();
        protected readonly byte _msgEncoderType;
        public MockChannelService(Uri url)
        {
            Url = url;
            _msgEncoderType = 0;
        }
        public bool Connected => throw new NotImplementedException();


        public Uri Url { get; private set; }
        public ChannelWriter<byte[]> ReceiverChannel { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public ChannelWriter<byte[]> SenderChannel => throw new NotImplementedException();

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
            var obytes = o.Encode(0);
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
