using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using AARC.Mesh;
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
            var transport = new MockTransportServer(null, new MockTransportFactory());
//            new MockChannelService("remote1:0"));
            //            transport.RegisterService(new MockQueueService("remote2:0"));

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

            for(var i = 0; i < 5; i++)
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
    }
}
