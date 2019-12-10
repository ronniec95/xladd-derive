using System;
using System.IO;
using AARC.Graph.Test;
using AARC.Mesh;
using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using Microsoft.VisualStudio.TestTools.UnitTesting;


namespace AARC.MeshTests
{
    /*
    [TestClass]
    public class DSSMUT
    {
        public enum GraphMethod1
        {
            newcloseprice,
            newrandom,
            method1
        }

        public enum GraphNewClosePrice
        {
            setcloseprice, newcloseprice
        }


        [TestMethod]
        public void TestDSSMCreate()
        {
            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            Assert.IsNotNull(dssm);
        }

        [TestMethod]
        public void TestInputQDeserialisation()
        {
            var InputQRoutesJSON = File.ReadAllText("InputQRoutes6002.json");
            var bytes = System.Text.Encoding.ASCII.GetBytes(InputQRoutesJSON);
            var i = new MeshMessage { PayLoad = InputQRoutesJSON };
            Assert.IsNotNull(i);

            Assert.AreEqual(i.QueueName, "getinputqs");
        }

        [TestMethod]
        public void TestOutputQDeserialisation()
        {
            var OutputQRoutesJSON = File.ReadAllText("OutputQRoutes6002.json");
            var bytes = System.Text.Encoding.ASCII.GetBytes(OutputQRoutesJSON);
            var o = new MeshMessage { PayLoad = OutputQRoutesJSON };

            Assert.AreEqual(o.QueueName, "getoutputqs");
        }

        [TestMethod]
        public void TestDSSMSetInputQ()
        {
            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            Assert.IsNotNull(dssm);

            dssm.inputQs.TryAdd("setcloseprice", null);


            Assert.IsFalse(dssm.inputQs.IsEmpty);
            Assert.IsTrue(dssm.outputQs.IsEmpty);
        }

        [TestMethod]
        public void TestDSSMSetOutputQ()
        {
            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            Assert.IsNotNull(dssm);

            dssm.outputQs.TryAdd("newcloseprice", null);

            Assert.IsTrue(dssm.inputQs.IsEmpty);
            Assert.IsFalse(dssm.outputQs.IsEmpty);
        }


        [TestMethod]
        public void TestDSSMSetInputQRoutes()
        {
            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            Assert.IsNotNull(dssm);

            var InputQRoutesJSON = File.ReadAllText("InputQRoutes6002.json");
            var bytes = System.Text.Encoding.ASCII.GetBytes(InputQRoutesJSON);
            var i = new DiscoveryMessage { Payload = InputQRoutesJSON };
            Assert.IsNotNull(i);

            Assert.IsTrue(dssm.inputQs.IsEmpty);
            Assert.IsTrue(dssm.outputQs.IsEmpty);

            Assert.IsTrue(dssm.InputQsRoutes.IsEmpty);
            Assert.IsTrue(dssm.OutputQsRoutes.IsEmpty);

            dssm.Receive(i);

            Assert.IsTrue(dssm.inputQs.IsEmpty);
            Assert.IsTrue(dssm.outputQs.IsEmpty);

            Assert.IsFalse(dssm.InputQsRoutes.IsEmpty);
            Assert.IsTrue(dssm.OutputQsRoutes.IsEmpty);
        }

        [TestMethod]
        public void TestDSSMSetOutputQRoutes()
        {
            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            Assert.IsNotNull(dssm);

            var OutputQRoutesJSON = File.ReadAllText("OutputQRoutes6002.json");
            var bytes = System.Text.Encoding.ASCII.GetBytes(OutputQRoutesJSON);
            var o = new DiscoveryMessage { Payload = OutputQRoutesJSON };
            Assert.IsNotNull(o);

            Assert.IsTrue(dssm.inputQs.IsEmpty);
            Assert.IsTrue(dssm.outputQs.IsEmpty);

            Assert.IsTrue(dssm.InputQsRoutes.IsEmpty);
            Assert.IsTrue(dssm.OutputQsRoutes.IsEmpty);

            dssm.Receive(o);

            Assert.IsTrue(dssm.inputQs.IsEmpty);
            Assert.IsTrue(dssm.outputQs.IsEmpty);

            Assert.IsTrue(dssm.InputQsRoutes.IsEmpty);
            Assert.IsFalse(dssm.OutputQsRoutes.IsEmpty);
        }

        [TestMethod]
        public void TestMeshWrapperCreate()
        {
            var wrapper = new ClosePriceQueueTransform();

            Assert.IsNotNull(wrapper);
        }

        [TestMethod]
        public void TestMeshWrapperDSSM()
        {
            var wrapper = new ClosePriceQueueTransform();

            Assert.IsNotNull(wrapper);

            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            Assert.IsNotNull(dssm);

            Assert.IsTrue(dssm.inputQs.IsEmpty);
            Assert.IsTrue(dssm.outputQs.IsEmpty);

            wrapper.RegisterDependencies(dssm.inputQs, dssm.outputQs);

            Assert.IsFalse(dssm.inputQs.IsEmpty);
            Assert.IsFalse(dssm.outputQs.IsEmpty);

            Assert.AreEqual(dssm.inputQs.Count, 1);
            Assert.AreEqual(dssm.outputQs.Count, 1);

            Assert.IsTrue(dssm.inputQs.ContainsKey("setcloseprice"));
            Assert.IsTrue(dssm.outputQs.ContainsKey("newcloseprice"));
        }

        [TestMethod]
        public void TestMeshWrapperDSSMAndInputRoute()
        {
            var wrapper = new ClosePriceQueueTransform();

            Assert.IsNotNull(wrapper);

            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            Assert.IsNotNull(dssm);

            Assert.IsTrue(dssm.inputQs.IsEmpty);
            Assert.IsTrue(dssm.outputQs.IsEmpty);

            Assert.IsTrue(dssm.InputQsRoutes.IsEmpty);
            Assert.IsTrue(dssm.OutputQsRoutes.IsEmpty);

            wrapper.RegisterDependencies(dssm.inputQs, dssm.outputQs);

            Assert.IsTrue(dssm.InputQsRoutes.IsEmpty);
            Assert.IsTrue(dssm.OutputQsRoutes.IsEmpty);

            Assert.IsFalse(dssm.inputQs.IsEmpty);
            Assert.IsFalse(dssm.outputQs.IsEmpty);

            Assert.AreEqual(dssm.inputQs.Count, 1);
            Assert.AreEqual(dssm.outputQs.Count, 1);

            Assert.IsTrue(dssm.inputQs.ContainsKey("setcloseprice"));
            Assert.IsTrue(dssm.outputQs.ContainsKey("newcloseprice"));

            var InputQRoutesJSON = File.ReadAllText("InputQRoutes6002.json");
            var bytes = System.Text.Encoding.ASCII.GetBytes(InputQRoutesJSON);
            var i = new DiscoveryMessage { Payload = InputQRoutesJSON };
            Assert.IsNotNull(i);

            dssm.Receive(i);

            Assert.IsFalse(dssm.InputQsRoutes.IsEmpty);
            Assert.IsTrue(dssm.OutputQsRoutes.IsEmpty);

            Assert.IsFalse(dssm.inputQs.IsEmpty);
            Assert.IsFalse(dssm.outputQs.IsEmpty);

            Assert.AreEqual(dssm.inputQs.Count, 1);
            Assert.AreEqual(dssm.outputQs.Count, 1);

            Assert.IsTrue(dssm.inputQs.ContainsKey("setcloseprice"));
            Assert.IsTrue(dssm.outputQs.ContainsKey("newcloseprice"));

            Assert.AreEqual(dssm.InputQsRoutes.Count, 1);
            Assert.IsTrue(dssm.InputQsRoutes.ContainsKey("setcloseprice"));
        }

        [TestMethod]
        public void TestMeshWrapperDSSMAndOutputRoute()
        {
            var wrapper = new ClosePriceQueueTransform();

            Assert.IsNotNull(wrapper);

            var dssm = new DiscoveryServiceStateMachine<MeshMessage>();
            Assert.IsNotNull(dssm);

            Assert.IsTrue(dssm.inputQs.IsEmpty);
            Assert.IsTrue(dssm.outputQs.IsEmpty);

            Assert.IsTrue(dssm.InputQsRoutes.IsEmpty);
            Assert.IsTrue(dssm.OutputQsRoutes.IsEmpty);

            wrapper.RegisterDependencies(dssm.inputQs, dssm.outputQs);

            Assert.IsTrue(dssm.InputQsRoutes.IsEmpty);
            Assert.IsTrue(dssm.OutputQsRoutes.IsEmpty);

            Assert.IsFalse(dssm.inputQs.IsEmpty);
            Assert.IsFalse(dssm.outputQs.IsEmpty);

            Assert.AreEqual(dssm.inputQs.Count, 1);
            Assert.AreEqual(dssm.outputQs.Count, 1);

            Assert.IsTrue(dssm.inputQs.ContainsKey("setcloseprice"));
            Assert.IsTrue(dssm.outputQs.ContainsKey("newcloseprice"));

            var OutputQRoutesJSON = File.ReadAllText("OutputQRoutes6002.json");
            var bytes = System.Text.Encoding.ASCII.GetBytes(OutputQRoutesJSON);
            var o = new DiscoveryMessage { Payload = OutputQRoutesJSON };
            Assert.IsNotNull(o);

            dssm.Receive(o);

            Assert.IsTrue(dssm.InputQsRoutes.IsEmpty);
            Assert.IsFalse(dssm.OutputQsRoutes.IsEmpty);

            Assert.IsFalse(dssm.inputQs.IsEmpty);
            Assert.IsFalse(dssm.outputQs.IsEmpty);

            Assert.AreEqual(dssm.inputQs.Count, 1);
            Assert.AreEqual(dssm.outputQs.Count, 1);

            Assert.IsTrue(dssm.inputQs.ContainsKey("setcloseprice"));
            Assert.IsTrue(dssm.outputQs.ContainsKey("newcloseprice"));

            Assert.AreEqual(dssm.OutputQsRoutes.Count, 1);
            Assert.IsTrue(dssm.OutputQsRoutes.ContainsKey("newcloseprice"));
        }
    }*/
}
