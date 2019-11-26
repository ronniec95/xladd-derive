using System;
using System.Net.Sockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    public class SocketServiceFactory
    {
        protected readonly IServiceProvider _serviceProvider;

        public SocketServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public SocketService Create()
        {
            return new SocketService(_serviceProvider.GetService<ILogger<SocketService>>());
        }

        public SocketService Create(Socket socket)
        {
            return new SocketService(socket, _serviceProvider.GetService<ILogger<SocketService>>());
        }
    }
}
