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
        private Uri _uri;
        private MeshMonitor _mm;

        public MeshTransportFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            _mm = new MeshMonitor(new Uri("tcp://ronniepc:9900"), this, serviceProvider.GetService<ILogger<MeshMonitor>>());
        }

        /// <summary>
        /// Using _serviceHost details to create connection
        /// </summary>
        /// <returns></returns>
        public IMeshServiceTransport Create() => Create(_uri);


        /// <summary>
        /// Uses servicedetails work work out how to setup this MeshQueueService
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IMeshServiceTransport Create(string uri)
        {
            _uri = new Uri(uri);

            return Create(_uri);
        }

        /// <summary>
        /// Creates a new SocketServices and reset _serviceHost details and starts ReadAsync
        /// </summary>
        /// <param name="uri">Host name or IP address</param>
        /// <returns></returns>
        public IMeshServiceTransport Create(Uri uri)
        {
            var qss = new SocketTransport(_serviceProvider.GetService<ILogger<SocketTransport>>());
            qss.ManageConnection(uri, false);
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

            throw new NotSupportedException($"Cant create a IMeshQueueService from {dispose.GetType()}");
        }

        public SocketTransport Create(Socket socket) => new SocketTransport(socket, _serviceProvider.GetService<ILogger<SocketTransport>>());

        public void MessageRelay(byte[] bytes) => _mm.OnNext(bytes);
    }
}
