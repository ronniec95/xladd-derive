using System.Threading.Channels;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AARC.Mesh.SubService
{
    public static class MeshServiceConfig
    {
        public static void Server(IConfiguration configuration, IServiceCollection services)
        {
            // Configuartion/options required by lower
            services.AddSingleton(new MeshConfig(configuration));
            // Used by services to publish to Smart Monitor.
            services.AddSingleton<Channel<byte[]>>(Channel.CreateUnbounded<byte[]>());
            // SKA Smart Monitor
            services.AddSingleton<IMonitor, MeshMonitor>();
            services.AddSingleton<DiscoveryServiceStateMachine<MeshMessage>>();
            services.AddSingleton<DiscoveryMonitor<DiscoveryMessage>>();
            services.AddSingleton<MeshServiceManager>();
        }
    }
}
