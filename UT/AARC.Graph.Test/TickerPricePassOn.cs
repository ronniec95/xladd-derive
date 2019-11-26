using System;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;

namespace AARC.Graph.Test
{
    public class TickerPricePassOn : MeshQueueMarshal
    {
        public TickerPricePassOn()
        {
            InputQueueNames = new string[] { "setticker" };
            OutputQueueNames = new string[] { "getticker" };
        }

        public override void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public override void OnNext(MeshMessage item)
        {
            throw new NotImplementedException();
        }
    }
}
