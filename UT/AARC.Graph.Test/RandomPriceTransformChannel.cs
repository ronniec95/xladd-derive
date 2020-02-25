using System;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Mesh;
using System.Collections.Generic;

namespace AARC.Graph.Test
{
    /// <summary>
    /// Example wrapper class for a simple Method of multiplication based on the subscription of two independently supplied subscriptions.
    /// </summary>
    /// <typeparam name="T">Using T to help with the Serialization/Deserialization - work in progrees</typeparam>
    public class RandomPriceTransformChannel : IMeshReactor<MeshMessage>
    {
        public RandomPriceTransformChannel()
        {
            //InputQueueNames = new string[] { GraphNewRamdomPrice.setrandomprice.ToString() };
            //OutputQueueNames = new string[] { GraphMethod1.newrandom.ToString() };
        }

        public void Start() { }

        public  void OnNext(MeshMessage item)
        {
            if (item != null)
            {
                var payLoad = item.PayLoad;
                // Use the T Deserializer
                var Request = TickerPrice.Deserialise(payLoad);
                Method1NewPrice(item.GraphId, item.XId, Request.Ticker);
            }
        }

        /// <summary>
        /// Call our Method1 if we have all the parameters and post the result back to anyone listening
        /// </summary>
        /// <param name="n">Ticker</param>
        public void Method1NewPrice(uint graphid, uint xid, string n)
        {
            var tp = new TickerPrice { Ticker = n, Price = ClosePrice(n) };
            var message = new MeshMessage { GraphId = graphid, XId = xid, PayLoad = tp.Serialize() };
            //PostOutputQueue?.Invoke(GraphMethod1.newrandom.ToString(), message);
        }

        protected Random random = new Random();

        public string Name => throw new NotImplementedException();

        public IList<IRouteRegister<MeshMessage>> ChannelRouters => throw new NotImplementedException();

        public double ClosePrice(string ticker)
        {
            return random.Next(0, 100);
        }
    }
}
