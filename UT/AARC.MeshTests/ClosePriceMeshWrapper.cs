using System;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using AARC.Mesh;
using System.Collections.Concurrent;
using static AARC.MeshTests.DSSMUT;


namespace AARC.MeshTests
{
    /// <summary>
    /// Example wrapper class for a simple Method of multiplication based on the subscription of two independently supplied subscriptions.
    /// </summary>
    /// <typeparam name="T">Using T to help with the Serialization/Deserialization - work in progrees</typeparam>
    public class ClosePriceMeshWrapper<T> : MeshQueueTransform<MeshMessage> where T : class
    {
        protected ConcurrentDictionary<string, double> _latestClosePrices = new ConcurrentDictionary<string, double>();

        public ClosePriceMeshWrapper()
        {
            InputRoutes = new string[] { GraphNewClosePrice.setcloseprice.ToString() };
            QueueOut = new string[] { GraphMethod1.newcloseprice.ToString() };
        }

        protected override void MessageProcessor(object item)
        {
            var message = item as MeshMessage;
            if (message != null)
            {
                var payLoad = message.PayLoad;
                // Use the T Deserializer
                var Request = TickerPrice.Deserialise<T>(payLoad);
                _latestClosePrices[Request.Ticker] = Request.Price;
                Method1NewPrice(message.GraphId, message.XId, Request.Ticker);
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
            PostOutQueue?.Invoke(GraphMethod1.newcloseprice.ToString(), message);
        }

        protected Random random = new Random();

        public double ClosePrice(string ticker)
        {
            return _latestClosePrices.ContainsKey(ticker) ? _latestClosePrices[ticker] : 0.0;
        }
    }
}
