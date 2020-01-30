using System;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.Extensions.DependencyInjection;

namespace AARC.Mesh.TCP
{
    public static class SocketServiceConfig
    {
        /// <summary>
        /// Underlying message transport using sockets
        /// </summary>
        /// <param name="services"></param>
        public static void Transport(IServiceCollection services)
        {
            services.AddSingleton<ServiceUrlFactory>();
            // MeshSocketServer needs the port it allows external services to connect on.
            services.AddSingleton<IMeshTransport<MeshMessage>, SocketServerTransport<MeshMessage>>();
            services.AddSingleton<IMeshTransportFactory, MeshTransportFactory>();
        }
    }
}
