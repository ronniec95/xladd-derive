using System;
using System.Collections.Generic;
using System.Linq;
using AARC.Mesh.AutoWireUp;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// NOTES: AVS 18/1/2020
// IMeshReactor provides Name, ChannelRouters and Start
// Start, commences listening (by creating observables) on method inputs(?), and creates observers on the ChannelRouters ??
// why is it not the other way around? creating observers on the method inputs, and observables for outputs?
// observer: does things on messages (OnNext, OnCompleted, OnError) - subscribes and unsubscribes
// observable: sends messages (OnNext, OnCompleted, OnError) - i.e. implements these methods
// ChannelRouters are Receiver/Publisher channels

// framework: hides this for us: mesh channel proxy is both an observer and observable:
// it subscribes and unsubscribes, and passes messages on by e.g. calling OnNext on its observers - ie sending payload
// it subscribes to "receiver" channels, and does something with Publisher channels - what?

// Consider some examples of IMeshReactor<>
// BiggestStocks:
//      on construction, gets repository, creates an observer with channel name and of type List<Stock>, sets ChannelRouters to be the observer
//      on Start, it goes to its repository (supplied on construction), obtains stocks, and calls OnNext on observer
// but should it not be an observable? calling OnNext, etc when new data is received?

// Anyway, in this pattern/paradigm - the DataFlowFactory - can create a service (by name) - e.g. BiggestStocksReactor or NasdaqTradableTickers
// MeshHostedService - reads config, creates _meshServices. On StartAsync, creates discovery service, calls (MeshServiceManager) RegisterChannels for each route in each service.ChannelRouters, and calls Start on each service
// MeshServiceManager.RegisterChannels - calls (on route): RegisterReceiverChannels (inputs channels) RegistePublisherChannels (output channels) and sets route.PublishChannel to "OnNext"
// 

// send and receive notifications 
// when a parameter comes through - the parameter might need to be associated in some way with other parameters
// for example: imagine we have something that listens on more than one input
// example 1: listens on prices for various different assets - and signals when conditions are met
// example 2: drift model - listens for various models to input, some are updating more regularly than others
//  - it would might probably wish to keep track of the latest - and discard the others
//  - when it has received all inputs, it can process - and then what happens to the parameters?

// use cases for various queues / queue types

// what about instead providing a way to call various methods etc, by reflection
// each method (node) - has a unique id, and a set of parameters, and types, and in effect, we wish to cast objects to the correct type?
// but in effect, with mi.Invoke we don't need to cast them - the method will do so
// - ie an object is fine - but it must be the right object, so the cast must have happened somewhere... where?

// for each method, set up observers on the (input) parameters? apparently not
// - subscribe to the observables on those parameters - who sets up the observables? erm.. ok maybe we do it here!? how do parameters come in - via payloads
// what calls what when a payload arrives? - a MeshMessage is received by a service. it is decoded into GraphId, XId, Service, Channel, Payload
// - when input parameters come in (ie callback is called - check to see if we have a full set of parameters)
// 

// subscribe to observables - when parameters arrive - determine whether a method can be called?
// if a method is called - get the outputs, and call OnNext on the observers?

namespace AARC.MeshTests
{
    public class Class1Simple
    {
        public static string ReturnString(string a)
        { return a;  }
    }

    public class ComplexClass
    {
        public static bool FirstMethod(string input1, int input2, double input3)
        {
            return input1.Length > input2 * input3;
        }

        public static T Flatten<T>(T[] array, int index)
        {
            // this makes no sense to me - how do you write this??
            // it seems to me that the framework would need to handle it...
            return array[index];
        }

        // some confusing method
        public static int SomeMethod<T>(Dictionary<string, double[]> dictionaryOfDoublesByString,
            out List<T> listOfTees, out string outputOne, out int outputTwo) where T : new()
        {
            List<T> tees = new List<T>();
            outputOne = string.Empty;
            outputTwo = 0;

            foreach (var key in dictionaryOfDoublesByString.Keys)
            {
                if (dictionaryOfDoublesByString[key].Average() > 0)
                {
                    outputOne = key;
                    outputTwo++;

                    T bob = new T();
                    tees.Add(bob);
                }
            }

            listOfTees = tees;

            return listOfTees.Count;
        }
    }
    [TestClass]
    public class AutoWireUpUT
    {
        [TestMethod]
        public void CreateListReflection()
        {
            var parameters = new object[] { };
            var atype = typeof(List<>);
            var constructed = atype.MakeGenericType(typeof(string));
            var instance = Activator.CreateInstance(constructed, parameters);
            Assert.IsInstanceOfType(instance, typeof(List<string>));
        }

        [TestMethod]
        public void CreateObservable()
        {
            var instance = new MeshObservable<string>("inputstring");
            Assert.IsInstanceOfType(instance, typeof(MeshObservable<string>));
        }

        [TestMethod]
        public void CreateObservableUsingReflection()
        {
            var parameters = new object[] { "inputstring"};
            var atype = typeof(MeshObservable<>);
            var constructed = atype.MakeGenericType(typeof(string));
            var instance = Activator.CreateInstance(constructed, parameters);
            Assert.IsInstanceOfType(instance, typeof(MeshObservable<string>));
        }

        [TestMethod]
        public void CreateObserver()
        {
            var instance = new MeshObserver<string>("outputstring");
            Assert.IsInstanceOfType(instance, typeof(MeshObserver<string>));
        }

        [TestMethod]
        public void CreateObserverUsingReflection()
        {
            var parameters = new object[] { "outputstring" };
            var atype = typeof(MeshObserver<>);
            var constructed = atype.MakeGenericType(typeof(string));
            var instance = Activator.CreateInstance(constructed, parameters);
            Assert.IsInstanceOfType(instance, typeof(MeshObserver<string>));
        }

        [TestMethod]
        public void CreateClass1SimpleWireUp()
        {
            var wireUp = new MeshMethodWireUp(typeof(Class1Simple), null);
            wireUp.Start();

            Assert.IsNotNull(wireUp.ChannelRouters);

            Assert.AreEqual<int>(2, wireUp.ChannelRouters.Count);

            var noInputRoutes = 0;
            var noOutputRoutes = 0;
            foreach (var routeRegister in wireUp.ChannelRouters)
            {
                if (!string.IsNullOrEmpty(routeRegister.InputChannelAlias))
                {
                    ++noInputRoutes;
                    Assert.IsInstanceOfType(routeRegister, typeof(MeshObservable<object>));
                }

                if (!string.IsNullOrEmpty(routeRegister.OutputChannelAlias))
                {
                    ++noOutputRoutes;
                    Assert.IsInstanceOfType(routeRegister, typeof(MeshObserver<object>));
                }

                // so we can send some things through on InputChannels
                // if we send enough parameters, should expect eventually the method will be invoked, and consequently,
                // we will receive an "OnNext" via the OutputChannel

                // but: how to send something in the input channel?
            }

            Assert.AreEqual<int>(1, noInputRoutes);
            Assert.AreEqual<int>(1, noOutputRoutes);
        }

        [TestMethod]
        public void TestWireUpComplex()
        {
            var wireUp = new MeshMethodWireUp(typeof(ComplexClass), null);
            wireUp.Start();

            Assert.AreNotEqual<int>(0, wireUp.ChannelRouters.Count);

            foreach (IRouteRegister<MeshMessage> routeRegister in wireUp.ChannelRouters)
            {
                if (!string.IsNullOrEmpty(routeRegister.InputChannelAlias))
                    Assert.IsInstanceOfType(routeRegister, typeof(MeshObservable<object>));

                if (!string.IsNullOrEmpty(routeRegister.OutputChannelAlias))
                    Assert.IsInstanceOfType(routeRegister, typeof(MeshObserver<object>));

                // so we can send some things through on InputChannels
                // if we send enough parameters, should expect eventually the method will be invoked, and consequently,
                // we will receive an "OnNext" via the OutputChannel

                // but: how to send something in the input channel?
            }
        }
    }
}
