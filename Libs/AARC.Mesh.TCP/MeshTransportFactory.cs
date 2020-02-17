using System;
using System.Net.Sockets;
using AARC.Mesh.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    public class MeshTransportFactory : IMeshTransportFactory
    {
        protected readonly IServiceProvider _serviceProvider;
        private Uri _url;
        private MeshMonitor _mm;

        public MeshTransportFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _mm = new MeshMonitor(serviceProvider.GetService<ILogger<MeshMonitor>>(), new Uri("udp://localhost:9900"));
        }

        /// <summary>
        /// Using _serviceHost details to create connection
        /// </summary>
        /// <returns></returns>
        public IMeshServiceTransport Create() => Create(_url);


        /// <summary>
        /// Uses servicedetails work work out how to setup this MeshQueueService
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public IMeshServiceTransport Create(string url)
        {
            _url = new Uri(url);

            return Create(_url);
        }

        /// <summary>
        /// Creates a new SocketServices and reset _serviceHost details and starts ReadAsync
        /// </summary>
        /// <param name="url">Host name or IP address</param>
        /// <returns></returns>
        public IMeshServiceTransport Create(Uri url)
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
        public IMeshServiceTransport Create(IDisposable dispose)
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

        public void MessageRelay(byte[] bytes) => _mm.OnNext(bytes);
    }
}
