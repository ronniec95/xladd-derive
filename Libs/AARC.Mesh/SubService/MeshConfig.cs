using System;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.Extensions.DependencyInjection;

namespace AARC.Mesh.SubService
{
    public static class MeshServiceConfig
    {
        public static void Server(IServiceCollection services)
        {
//            services.AddSingleton<ServiceHostNameFactory>();
            services.AddSingleton<DiscoveryServiceStateMachine<MeshMessage>>();
            services.AddSingleton<DiscoveryMonitor<DiscoveryMessage>>();
            services.AddSingleton<MeshServiceManager>();
            // MeshSocketServer needs the port it allows external services to connect on.
            //            services.AddSingleton<IMeshTransport<MeshMessage>, MeshSocketServer<MeshMessage>>();
            //            services.AddSingleton<IMeshQueueServiceFactory, SocketServiceFactory>();
        }
    }
}
