using System;
using System.Net.Sockets;
using System.Threading.Channels;
using AARC.Mesh.Interface;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    public class MeshTransportFactory : IMeshTransportFactory
    {
        protected readonly IServiceProvider _serviceProvider;
        private Uri _uri;

        public MeshTransportFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// Using _serviceHost details to create connection
        /// </summary>
        /// <returns></returns>
//        public IMeshServiceTransport Create() => Create(_uri);

        /// <summary>
        /// Uses servicedetails work work out how to setup this MeshQueueService
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public IMeshServiceTransport Create(string uri, ChannelWriter<byte[]> channelWriter)
        {
            _uri = new Uri(uri);

            return Create(_uri, channelWriter);
        }

        /// <summary>
        /// Creates a new SocketServices and reset _serviceHost details and starts ReadAsync
        /// </summary>
        /// <param name="uri">Host name or IP address</param>
        /// <returns></returns>
        public IMeshServiceTransport Create(Uri uri, ChannelWriter<byte[]> channelWriter)
        {
            var serviceTransport = new SocketTransport(_serviceProvider.GetService<IMonitor>(), _serviceProvider.GetService<ILogger<SocketTransport>>());
            serviceTransport.ManageConnection(uri, false);
            serviceTransport.ReceiverChannel = channelWriter;
            serviceTransport.ReadAsync();
            return serviceTransport;
        }

        /// <summary>
        /// Attaches a socket to a socketservice and starts async reads
        /// </summary>
        /// <param name="dispose"></param>
        /// <returns></returns>
        public IMeshServiceTransport Create(IDisposable dispose, ChannelWriter<byte[]> channelWriter)
        {
            var socket = dispose as Socket;
            if (socket != null)
            {
                var serviceTransport = Create(socket);
                serviceTransport.ReceiverChannel = channelWriter;
                serviceTransport.ReadAsync();
                return serviceTransport;
            }

            throw new NotSupportedException($"Cant create a IMeshQueueService from {dispose.GetType()}");
        }

        public SocketTransport Create(Socket socket) => new SocketTransport(socket, _serviceProvider.GetService<IMonitor>(), _serviceProvider.GetService<ILogger<SocketTransport>>());
    }
}
