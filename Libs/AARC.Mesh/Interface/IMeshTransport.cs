using System;
using System.Threading;
using System.Threading.Tasks;

namespace AARC.Mesh.Interface
{
    public interface IMeshTransport<T> : IObservable<T>, IObserver<T>, IDisposable where T: IMeshMessage
    {
        /// <summary>
        /// A unique id to identify this service for connected services
        /// </summary>
        Uri URI { get; }

        /// <summary>
        /// Helps with setting up the sequence of montor so that we can get hold of
        /// a valid URI
        /// </summary>
        /// <param name="port"></param>
        void SetPort(int port);

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
        Task StartListeningServices(CancellationToken cancellationToken);

        /// <summary>
        /// Register subscription
        /// </summary>
        /// <param name="transportUrl"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>new connection and connected</returns>
        bool ServiceConnect(Uri transportUrl, CancellationToken cancellationToken);
    }
}
