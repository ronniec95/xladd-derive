using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using AARC.Model;
using AARC.Model.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace AARC.MeshTests
{
    [TestClass]
    public class MSMUT
    {
        [TestMethod]
        public void TestMsM()
        {
            var sentdata = new ConcurrentDictionary<string, ConcurrentQueue<IAarcPrice>>();
            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            var transport = new MockTransportServer();
            transport.RegisterService(new MockQueueService("remote1:0"));
            //            transport.RegisterService(new MockQueueService("remote2:0"));

            var msm = new MeshServiceManager(new NullLogger<MeshServiceManager>(), dssm, new DiscoveryMonitor<DiscoveryMessage>(new NullLogger<DiscoveryMonitor<DiscoveryMessage>>(), null), transport);
            Assert.IsNotNull(msm);

            var omarshal = new MeshChannelProxy<Dictionary<string, TickerPrices>>(inputq: "nasdaqtestout");
            var nssdaqObservableSubscriber = new MeshObservable<Dictionary<string, TickerPrices>>(omarshal);
            msm.RegisterChannels(nssdaqObservableSubscriber);

            var imarshal = new MeshChannelProxy<IList<string>>(outputq: "nasdaqtestin");
            var nasdaqTickerObserverPublisher = new MeshObserver<IList<string>>(imarshal);
            msm.RegisterChannels(nasdaqTickerObserverPublisher);

            var routemap = new Dictionary<string, HashSet<string>>();
            routemap["nasdaqtestin"] = new HashSet<string> { "remote1:0" };
            dssm.Receive(new DiscoveryMessage { State = DiscoveryMessage.DiscoveryStates.GetOutputQs, Payload = JsonConvert.SerializeObject(routemap) });

            routemap.Clear();
            routemap["nasdaqtestout"] = new HashSet<string> { "remote1:0" };
            dssm.Receive(new DiscoveryMessage { State = DiscoveryMessage.DiscoveryStates.GetInputQs, Payload = JsonConvert.SerializeObject(routemap) });

            Assert.AreEqual(dssm.InputChannelRoutes.Count, 1);
            Assert.AreEqual(dssm.OutputChannelRoutes.Count, 1);


            nssdaqObservableSubscriber.Subscribe((tickerUniverse) =>
            {
                foreach (var kv in tickerUniverse)
                {
                    Console.WriteLine($"{kv.Key} update");
                    if (!sentdata.ContainsKey(kv.Key))
                        sentdata[kv.Key] = new ConcurrentQueue<IAarcPrice>();

                    sentdata[kv.Key].Enqueue(kv.Value);
                }
            });

            for (var i = 0; i < 10; i++)
            {
                Console.WriteLine("Ticker update");
                var tickers = new List<string> { "AAPL" };
                nasdaqTickerObserverPublisher.OnNext(tickers);
            }

            Assert.IsNotNull(sentdata);
            Assert.IsFalse(sentdata.IsEmpty);
            Assert.IsNotNull(sentdata["AAPL"]);
            Assert.AreEqual(10, sentdata["AAPL"].Count);
        }
    }
}
