using System;
using Microsoft.Extensions.Configuration;

namespace AARC.Mesh.Model
{
    public class MeshConfig
    {
        public MeshConfig(IConfiguration configuration)
        {
            DiscoveryService = new Uri(configuration.GetValue<string>("ds", "tcp://localhost:9999"));
            SmartMonitor = new Uri(configuration.GetValue<string>("sm", "tcp://localhost:9900"));

            ListeningPort = configuration.GetValue<Int32>("port", 0);

            Services = configuration.GetValue<string>("services", null);
        }

        public Uri DiscoveryService { get; }
        public Uri SmartMonitor { get; }
        public int ListeningPort { get; }
        public string Services { get; }
    }
}
