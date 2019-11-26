using System;
using System.Threading;
using System.Threading.Tasks;
using AARC.Mesh.Model;

namespace AARC.Mesh.Interface
{
    public interface IMeshMessage
    {
        byte[] Encode();

        IMeshMessage Decode(byte[] bytes);
    }

    public interface IMeshTransport<T> : IDisposable where T: IMeshMessage
    {
        /// <summary>
        /// A unique id to identify this service for connected services
        /// </summary>
        string TransportId { get; }

        /// <summary>
        /// Subscribe to incoming service messages
        /// </summary>
        Action<T> Subscribe { get; set; }

        /// <summary>
        /// Publish out going messages to connected services
        /// </summary>
        /// <param name="transportId"></param>
        /// <param name="message"></param>
        void Publisher(string transportId, T message);

        /// <summary>
        /// Cancel transport and shutdonw
        /// </summary>
        /// <returns></returns>
        Task Cancel();

        /// <summary>
        /// Listen for clients who want to subscibe to a queue
        /// </summary>
        /// <param name="port"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task StartListeningServices(int port, CancellationToken cancellationToken);

        /// <summary>
        /// Register subscription
        /// </summary>
        /// <param name="serverDetails"></param>
        /// <param name="cancellationToken"></param>
        void ServiceConnect(string serverDetails, CancellationToken cancellationToken);
    }
}
