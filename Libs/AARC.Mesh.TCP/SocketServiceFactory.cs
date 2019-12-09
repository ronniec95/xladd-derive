using System;
using System.Net.Sockets;
using AARC.Mesh.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    public class SocketServiceFactory : IMeshQueueServiceFactory
    {
        protected readonly IServiceProvider _serviceProvider;
        private Uri _url;

        public SocketServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Using _serviceHost details to create connection
        /// </summary>
        /// <returns></returns>
        public IMeshChannelService Create() => Create(_url);


        /// <summary>
        /// Uses servicedetails work work out how to setup this MeshQueueService
        /// </summary>
        /// <param name="servicedetails"></param>
        /// <returns></returns>
        public IMeshChannelService Create(string servicedetails)
        {
            _url = new Uri(servicedetails);

            return Create(_url);
        }

        /// <summary>
        /// Creates a new SocketServices and reset _serviceHost details and starts ReadAsync
        /// </summary>
        /// <param name="url">Host name or IP address</param>
        /// <returns></returns>
        public SocketTransport Create(Uri url)
        {
            var qss = new SocketTransport(_serviceProvider.GetService<ILogger<SocketTransport>>());
            qss.ManageConnection(url, false);
            qss.ReadAsync();
            return qss;
        }

        /// <summary>
        /// Attaches a socket to a socketservice and starts async reads
        /// </summary>
        /// <param name="dispose"></param>
        /// <returns></returns>
        public IMeshChannelService Create(IDisposable dispose)
        {
            var socket = dispose as Socket;
            if (socket != null)
            {
                var qss = Create(socket);
                qss.ReadAsync();
                return qss;
            }

            throw new NotSupportedException($"Cant not create a IMeshQueueService from {dispose.GetType()}");
        }

        public SocketTransport Create(Socket socket) => new SocketTransport(socket, _serviceProvider.GetService<ILogger<SocketTransport>>());
    }
}
