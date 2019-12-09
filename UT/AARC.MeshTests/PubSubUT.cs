using System;
using AARC.Mesh.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Reactive.Linq;
using AARC.Mesh.Interface;

namespace AARC.MeshTests
{
    [TestClass]
    public class PubSubUT
    {
        private MeshChannelProxy<IList<string>> marshal;

        [TestInitialize]
        public void Initialise()
        {
            marshal = new MeshChannelProxy<IList<string>>("testin", "testout");
        }

        [TestMethod]
        public void TestMeshSubscriber()
        {
            var testdata = new Queue<IList<string>>();
            var MyStringSubsciber = new MeshObservable<IList<string>>(marshal);

            using (var handle = MyStringSubsciber.Subscribe((m) => { testdata.Enqueue(m); }))
            {
                SimulateMeshReceiveMessages(marshal);

                Assert.AreEqual<int>(100, testdata.Count);
            }
        }

        [TestMethod]
        public void TestMeshThrottle()
        {
            var testdata = new Queue<IList<string>>();
            var myStringSubscriber = new MeshObservable<IList<string>>(marshal);

            using (var handle = myStringSubscriber.Throttle(TimeSpan.FromMilliseconds(600)).Subscribe((m) => { testdata.Enqueue(m); }))
            {
                SimulateMeshReceiveMessages(marshal);

                Assert.AreEqual<int>(0, testdata.Count);
            }
        }

        [TestMethod]
        public void TestMeshBuffer()
        {
            var testdata = new Queue<IList<IList<string>>>();
            var MyStringReceiver = new MeshObservable<IList<string>>(marshal);

            var handle = MyStringReceiver.Buffer(TimeSpan.FromMilliseconds(600)).Subscribe((m) => { testdata.Enqueue(m); });
            SimulateMeshReceiveMessages(marshal);


            Assert.AreEqual<int>(0, testdata.Count);
        }

        public void SimulateMeshReceiveMessages(MeshChannelProxy<IList<string>> sender)
        {
            for (var i = 0; i < 100; i++)
                sender.OnNext(new MeshMessage { PayLoad = $"[ \"Test{i}\" ]" });
        }

        [TestMethod]
        public void TestMeshPublisher()
        {
            var MyStringSender = new MeshObserver<IList<string>>(marshal);
          
            SimulateMeshSendMessages(MyStringSender);
        }

        private void SimulateMeshSendMessages(MeshObserver<IList<string>> sender)
        {

            var outMyStringObserver = new MeshDictionary<MeshMessage>();
            var messages = new Queue<MeshMessage>();

            sender.RegistePublisherChannels(outMyStringObserver);
            marshal.PublishChannel += (action, msg) =>
            {
                Assert.AreEqual<string>("testout", action);
                messages.Enqueue(msg);
            };

            for (var i = 0; i < 100; i++)
                sender.OnNext(new List<string> { $"Test{i}" });

            Assert.AreEqual<int>(100, messages.Count);
        }

    }
}
