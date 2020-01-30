using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using AARC.Mesh;
using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using AARC.Model.Interfaces;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Reactive.Linq;
using System;

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
            var transport = new MockTransportServer(null, new MockTransportFactory());


            var msm = new MeshServiceManager(new NullLogger<MeshServiceManager>(), dssm, new DiscoveryMonitor<DiscoveryMessage>(new NullLogger<DiscoveryMonitor<DiscoveryMessage>>(), null), transport);
            Assert.IsNotNull(msm);

            var receiveSubscriber = new MeshObservable<int>("receivechannel");
            msm.RegisterChannels(receiveSubscriber);

            var sendPublisher = new MeshObserver<string>("sendchannel");
            msm.RegisterChannels(sendPublisher);

            var routemap = new Dictionary<string, HashSet<string>>();
            routemap[receiveSubscriber.InputChannelAlias] = new HashSet<string> { "remote1:0" };
            dssm.CreateReceiveMessage(new DiscoveryMessage { State = DiscoveryMessage.DiscoveryStates.GetOutputQs, Payload = JsonConvert.SerializeObject(routemap) });

            routemap.Clear();
            routemap[sendPublisher.OutputChannelAlias] = new HashSet<string> { "remote1:0" };
            dssm.CreateReceiveMessage(new DiscoveryMessage { State = DiscoveryMessage.DiscoveryStates.GetInputQs, Payload = JsonConvert.SerializeObject(routemap) });

            Assert.AreEqual(dssm.LocalInputChannels.Count, 1);
            Assert.AreEqual(dssm.OutputChannelRoutes.Count, 1);


            receiveSubscriber.Subscribe(message =>
            {
                sendPublisher.OnNext($"{message}");
            },
            e => {
                // ToDo Should tell DS probably from within the Transport
                sendPublisher.OnError(e);
            }
            );

            for (var i = 0; i < 5; i++)
            {
                var mm = new MeshMessage
                {
                    GraphId = 0,
                    XId = MeshUtilities.NewXId,
                    Service = "tcp:/localhost:1000",
                    Channel = receiveSubscriber.InputChannelAlias,
                    PayLoad = JsonConvert.SerializeObject(i)
                };

                var message = mm.Encode();

                transport.OnPublish(message);
            }
        }

        [TestMethod]
        public void TestDictionary()
        {
            var sentdata = new ConcurrentDictionary<string, ConcurrentQueue<IAarcPrice>>();
            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            var transport = new MockTransportServer(null, new MockTransportFactory());


            var msm = new MeshServiceManager(new NullLogger<MeshServiceManager>(), dssm, new DiscoveryMonitor<DiscoveryMessage>(new NullLogger<DiscoveryMonitor<DiscoveryMessage>>(), null), transport);
            Assert.IsNotNull(msm);

            // Subscribe to portfolio tag/list of tickers
            // Example
            // folio0 = ["AAPL", "MSFT"]
            
            var receiveSubscriber = new MeshObservable<Tuple<string,HashSet<string>>>("receivechannel");
            msm.RegisterChannels(receiveSubscriber);

            var sendPublisher = new MeshObserver<Tuple<string, Tuple<string, List<double>>>>("sendchannel");
            msm.RegisterChannels(sendPublisher);

            var routemap = new Dictionary<string, HashSet<string>>();
            routemap[receiveSubscriber.InputChannelAlias] = new HashSet<string> { "tcp:/localhost:0" };
            dssm.CreateReceiveMessage(new DiscoveryMessage { State = DiscoveryMessage.DiscoveryStates.GetOutputQs, Payload = JsonConvert.SerializeObject(routemap) });

            routemap.Clear();
            routemap[sendPublisher.OutputChannelAlias] = new HashSet<string> { "tcp:/localhost:0" };
            dssm.CreateReceiveMessage(new DiscoveryMessage { State = DiscoveryMessage.DiscoveryStates.GetInputQs, Payload = JsonConvert.SerializeObject(routemap) });

            Assert.AreEqual(dssm.LocalInputChannels.Count, 1);
            Assert.AreEqual(dssm.OutputChannelRoutes.Count, 1);

            var dictionarySet = new ConcurrentDictionary<string, HashSet<string>>();
            receiveSubscriber.Subscribe(message =>
            {
                var key = message.Item1;
                HashSet<string> changeDeleted = null;
                HashSet<string> changeAdded = null;
                if (!dictionarySet.ContainsKey(key))
                // New List
                {
                    dictionarySet[key] = message.Item2;
                    changeAdded = message.Item2;
                }
                else
                {
                    var setOfValuesUpdate = message.Item2;
                    var setOfValues = dictionarySet[key];
                    var intersected = setOfValues.Intersect(setOfValuesUpdate);
                    changeDeleted = setOfValues.Except(setOfValuesUpdate).ToHashSet();
                    changeAdded = setOfValuesUpdate.Except(setOfValues).ToHashSet();
                    setOfValues.IntersectWith(intersected);
                    setOfValues.UnionWith(changeAdded);
                }

                // We can use a parallel for loop to get prices - 
                foreach (var value in changeAdded)
                {
                    // This is wrong if we use a ms
                    // We may hold all Ticker Prices
                    var prices = GetPrices(value);

                    // I would like to separate these
                    var vols = GetVols(prices);
                    var payload = new Tuple<string, Tuple<string, List<double>>>(key, new Tuple<string, List<double>>(value, prices));
                    sendPublisher.OnNext(payload);
                }

                /*
if (!multiPortfolioPrices.ContainsKey(porfolioTag))
    multiPortfolioPrices[porfolioTag] = new ConcurrentDictionary<string, List<double>>();

var portfolioOfPrices = multiPortfolioPrices[porfolioTag];
foreach (var ticker in message.Item2)
{
    var listPrices = GetPrices(ticker);
    portfolioOfPrices[ticker] = listPrices;
}*/
            },
            e => {
                // ToDo Should tell DS probably from within the Transport
                sendPublisher.OnError(e);
            }
            );

            var tickers = new HashSet<string>( new [] { "AAPL", "MSFT" });
            var tag = "folio";
            for (var i = 0; i < 5; i++)
            {
                var payload = new Tuple<string, HashSet<string>>(tag, tickers);
                var mm = new MeshMessage
                {
                    GraphId = 0,
                    XId = MeshUtilities.NewXId,
                    Service = "tcp:/localhost:1000",
                    Channel = receiveSubscriber.InputChannelAlias,                    
                    PayLoad = JsonConvert.SerializeObject(payload)
                };

                var message = mm.Encode();

                transport.OnPublish(message);

                tickers.Add("TESLA");
                
            }
        }

        static ConcurrentDictionary<string, List<double>> tickerPrices = new ConcurrentDictionary<string, List<double>>();
        public static List<double> GetPrices(string ticker)
        {
            if (!tickerPrices.ContainsKey(ticker))
                // Get it - Should be observing.
                tickerPrices[ticker] = new List<double>(100);

            // We already have it
            return tickerPrices[ticker];
        }

        public static List<double> GetVols(IList<double> prices)
        {
            return new List<double>(100);
        }

        [TestMethod]
        public void TestMeshSetObserable()
        {
            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            var transport = new MockTransportServer(null, new MockTransportFactory());


            var msm = new MeshServiceManager(new NullLogger<MeshServiceManager>(), dssm, new DiscoveryMonitor<DiscoveryMessage>(new NullLogger<DiscoveryMonitor<DiscoveryMessage>>(), null), transport);
            Assert.IsNotNull(msm);

            // Subscribe to portfolio tag/list of tickers
            // Example
            // folio0 = ["AAPL", "MSFT"]

            var subscriber = new MeshSetObserverable<string>("receivechannel");
            msm.RegisterChannels(subscriber);

            var routemap = new Dictionary<string, HashSet<string>>();
            routemap[subscriber.InputChannelAlias] = new HashSet<string> { "tcp:/localhost:0" };
            dssm.CreateReceiveMessage(new DiscoveryMessage { State = DiscoveryMessage.DiscoveryStates.GetOutputQs, Payload = JsonConvert.SerializeObject(routemap) });

            subscriber.Subscribe((x) =>
            {
                var changes = subscriber.Changes(x);
                Console.WriteLine(changes.Added);
                Console.WriteLine(changes.Deleted);
            });

            var tickers = new HashSet<string>(new[] { "AAPL", "MSFT" });
            for (var i = 0; i < 5; i++)
            {
                var payload = tickers;
                var mm = new MeshMessage
                {
                    GraphId = 0,
                    XId = MeshUtilities.NewXId,
                    Service = "tcp:/localhost:1000",
                    Channel = subscriber.InputChannelAlias,
                    PayLoad = JsonConvert.SerializeObject(payload)
                };

                var message = mm.Encode();

                transport.OnPublish(message);

                tickers.Add($"Test{i}");

            }
        }

    }
}
