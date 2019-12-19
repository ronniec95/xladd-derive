using System;
using System.Collections.Generic;
using AARC.Mesh;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;

namespace AARC.Graph.Test
{
    public class TickerPricePassOn : IMeshReactor<MeshMessage>
    {
        public TickerPricePassOn()
        {
        }

        public string Name => throw new NotImplementedException();

        public IList<IRouteRegister<MeshMessage>> ChannelRouters => throw new NotImplementedException();

        public void Start() { }
    }
}
