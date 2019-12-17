using System.Collections.Concurrent;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Mesh;
using System;
using System.Collections.Generic;

namespace AARC.Graph.Test
{
    /// <summary>
    /// Example wrapper class for a simple Method of multiplication based on the subscription of two independently supplied subscriptions.
    /// </summary>
    /// <typeparam name="T">Using T to help with the Serialization/Deserialization - work in progrees</typeparam>
    public class Method1QueueTransform : IMeshReactor<MeshMessage>
    {
        private ConcurrentDictionary<string, double> lastprice;
        private ConcurrentDictionary<string, double> lastrandom;

        public string Name => throw new NotImplementedException();

        public IList<IRouteRegister<MeshMessage>> ChannelRouters => throw new NotImplementedException();

        public Method1QueueTransform()
        {
            lastprice = new ConcurrentDictionary<string, double>();
            lastrandom = new ConcurrentDictionary<string, double>();
            //InputQueueNames = new string[] { GraphMethod1.newcloseprice.ToString(), GraphMethod1.newrandom.ToString() };
            //OutputQueueNames = new string[] { GraphMethod1.method1.ToString() };
        }
        public  void OnNext(MeshMessage item)
        {
            try
            {
                if (item != null)
                {
                    var action = Enum.Parse(typeof(GraphMethod1), item.Channel);

                    var payLoad = item.PayLoad;
                    // Use the T Deserializer
                    var price = TickerPrice.Deserialise(payLoad);

                    switch (action)
                    {
                        case GraphMethod1.newcloseprice:
                            Method1NewPrice(item.GraphId, item.XId, price.Ticker, price.Price);
                            break;
                        case GraphMethod1.newrandom:
                            Method1NewRandom(item.GraphId, item.XId, price.Ticker, price.Price);
                            break;
                            // Todo: Ignore unknown messages?
                    }
                }
            }
            catch(Exception e)
            {
                // Todo: How do we signal errors in processing?
            }
        }

        /// <summary>
        /// Call our Method1 if we have all the parameters and post the result back to anyone listening
        /// </summary>
        /// <param name="n">Ticker</param>
        /// <param name="v">Value</param>
        public void Method1NewPrice(uint graphid, uint xid, string n, double v)
        {
            lastprice[n] = v;
            if (lastrandom.ContainsKey(n))
            {
                var tp = new TickerPrice { Ticker = n, Price = Method1(v, lastrandom[n]) };
                var message = new MeshMessage { GraphId = graphid, XId = xid, PayLoad = tp.Serialize() };
                //PostOutputQueue?.Invoke(GraphMethod1.method1.ToString(), message);
            }
        }

        /// <summary>
        /// Call our Method1 if we have all the parameters and post the result back to anyone listening
        /// </summary>
        /// <param name="n">Ticker</param>
        /// <param name="v">Value</param>
        public void Method1NewRandom(uint graphid, uint xid, string n, double v)
        {
            lastrandom[n] = v;
            if (lastprice.ContainsKey(n))
            {
                var tp = new TickerPrice { Ticker = n, Price = Method1(lastprice[n], v) };
                var message = new MeshMessage { GraphId = graphid, XId = xid, PayLoad = tp.Serialize() };
                //PostOutputQueue?.Invoke(GraphMethod1.method1.ToString(), message);
            }
        }

        /// <summary>
        /// This could be a wrapper to a class/methods
        /// </summary>
        /// <param name="price"></param>
        /// <param name="random"></param>
        /// <returns></returns>
        public double Method1(double price, double random) => price * random;

    }
}
