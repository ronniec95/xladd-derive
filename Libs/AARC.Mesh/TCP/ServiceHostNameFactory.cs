using System;
using System.Net;
using AARC.Mesh.Model;

namespace AARC.Mesh.TCP
{
    public class ServiceHostNameFactory
    {
        protected readonly ServiceHost _serviceHost;
        public ServiceHostNameFactory()
        {
            _serviceHost = new ServiceHost
            {
                HostName = Dns.GetHostName()
            };
        }

        public string TransportId => _serviceHost.ToString();
    }
}
