using System;
using System.Reflection;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.AutoWireUp
{
    public class AutoWireUpFactory : IMeshNodeFactory
    {
        private IServiceProvider _provider;

        public AutoWireUpFactory(IServiceProvider provider)
        {
            _provider = provider;
        }

        public IMeshReactor<MeshMessage> Get(string service)
        {
            Type t = Type.GetType(service);
            var logger = _provider.GetService<ILogger<MeshMethodWireUp>>();
            var x = new MeshMethodWireUp(t, logger);

            return x;
        }
    }
}
