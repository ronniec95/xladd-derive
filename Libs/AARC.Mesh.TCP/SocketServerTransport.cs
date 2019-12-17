using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AARC.Mesh.Interface;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    /// <summary>
    /// A socket server that creates socketservice classes when client services connect.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SocketServerTransport<T> : ObserverablePattern<T>, IMeshTransport<T>, IPublisher<byte[]> where T : IMeshMessage, new()
    {
        private readonly CancellationTokenSource _localCancelSource;
        private readonly CancellationToken _localct;

        private readonly ManualResetEvent _listenAcceptEvent;

        private readonly ConcurrentDictionary<string, IMeshServiceTransport> _meshServices;

        private readonly ILogger _logger;

        private readonly IMeshTransportFactory _qServiceFactory;

        public int MonitorPeriod { get; set; }

        //public Action<T> Subscribe { get; set; }

        private Uri _url;

        public string Url { get { return _url?.ToString(); } }

        public SocketServerTransport(ILogger<SocketServerTransport<T>> logger, IMeshTransportFactory qServiceFactory)
        {
            _localCancelSource = new CancellationTokenSource();
            _listenAcceptEvent = new ManualResetEvent(false);
            _logger = logger;
            _qServiceFactory = qServiceFactory;
            _meshServices = new ConcurrentDictionary<string, IMeshServiceTransport>();
            MonitorPeriod = 15000;
            _localct = _localCancelSource.Token;
        }

        public async Task StartListeningServices(int port, CancellationToken cancellationToken) => await Task.Factory.StartNew(() => Listen(port, cancellationToken));

        public async Task Cancel()
        {
            await Task.Factory.StartNew(() =>
            {
                _localCancelSource.Cancel();
                _logger?.LogInformation("Cancelled");
            });
        }

        /// <summary>
        /// Look up to see if we have a mesh service with the service details.
        /// If not create one. servicedetails should be enough to create
        /// </summary>
        /// <param name="servicedetails"></param>
        /// <param name="cancellationToken"></param>
        public void ServiceConnect(string servicedetails, CancellationToken cancellationToken)
        {
            if (_meshServices.ContainsKey(servicedetails))
            {
                var service = _meshServices[servicedetails];
                if (service.ConnectionAlive())
                    return;

                if (_meshServices.TryRemove(servicedetails, out service))
                    service.Dispose();
            }

            _logger.LogInformation($"Creating a connecting to {servicedetails}");
            var qss = _qServiceFactory.Create(servicedetails);
            _meshServices[servicedetails] = qss;
        }

        public void Listen(int port, CancellationToken cancellationToken)
        {

            _url = NetworkExt.GetHostNameUrl(port);

            var ipAddress = NetworkExt.GetHostIPAddress(_url.Host);
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            try
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localct, cancellationToken))
                {
                    var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _logger?.LogDebug($"Created Listener on {port}");
                    // Bind the socket to the local endpoint and   
                    // listen for incoming connections.  
                    listener.Bind(localEndPoint);
                    listener.Listen(10);
                    _ = MonitorStaleConnectedServices();
                    // Start an asynchronous socket to listen for connections.
                    while (!linkedCts.IsCancellationRequested)
                    {
                        _listenAcceptEvent.Reset();
                        _logger?.LogInformation($"[{ipAddress}:{port}] Accepting connections...");
                        listener.BeginAccept(new AsyncCallback(NewConnectionCallback), listener);
                        _listenAcceptEvent.WaitOne();
                    }
                }
            }
            catch (Exception e)
            {
                _logger?.LogCritical(e.ToString());
            }
            finally
            {
                if (!_localct.IsCancellationRequested)
                    _logger?.LogInformation("Cancel requested");
            }
        }

        /// <summary>
        /// When a new connection is made the socket is passed to the servicefactory
        /// to create a meshservice and added to the _meshServices list for
        /// full DUPLEX comms
        /// </summary>
        /// <param name="ar"></param>
        private void NewConnectionCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            _listenAcceptEvent.Set();
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            var socket = listener.EndAccept(ar);

            // Create the state object.  
            var service = _qServiceFactory.Create(socket);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _logger?.LogInformation($"[{service.Url}]: New Connection");
            service.Subscribe(this);
        }

        /// <summary>
        /// Check if service is a live and drop if not
        /// </summary>
        /// <param name="qService">connected to a network service</param>
        protected bool DropClosedConnections(IMeshServiceTransport qService)
        {
            try
            {
                // Is socket still connected
                if (!qService.ConnectionAlive())
                {
                    qService.Dispose();
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"MSS UNKNOWN ERROR");
            }
            return false;
        }

        protected void ShutdownConnection(IMeshServiceTransport socketService)
        {
            if (socketService != null)
                socketService.Shutdown();
        }

        private async Task MonitorStaleConnectedServices()
        {
            await Task.Factory.StartNew(() =>
            {
                while (!_localct.IsCancellationRequested)
                {
                    _logger?.LogDebug($"MSS MonitoringSockets connections[{_meshServices.Count}]");
                    foreach (var kvp in _meshServices)
                    {
                        var service = kvp.Value;
                        if (DropClosedConnections(service))
                        {
                            IMeshServiceTransport ss;
                            if (_meshServices.TryRemove(kvp.Key, out ss))
                            {
                                _logger?.LogInformation($"MSS dropped connections[{ss.Url}]");
                            }
                            else
                                _logger?.LogInformation($"MSS failed to drop [{service.Url}]");
                        }

                    }

                    Task.Delay(MonitorPeriod, _localct).Wait();
                }
            });
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(T value)
        {
            var bytes = value.Encode();
            foreach (var transportId in value.Routes)
                if (_meshServices.ContainsKey(transportId))
                    if (_meshServices.ContainsKey(transportId))
                    {
                        var service = _meshServices[transportId];
                        if (service.Connected)
                            _meshServices[transportId].OnPublish(bytes);
                        else
                        {
                            // Todo: What if we cant remove?
                            _logger.LogInformation($"{transportId}: Disconnected - Removing");
                            if (_meshServices.TryRemove(transportId, out service))
                                service.Dispose();
                        }
                    }
                    else
                    _logger.LogInformation($"{transportId}: NO ROUTES available");
        }

        #region Subscriber
        public List<IPublisher<byte[]>> _publishers = new List<IPublisher<byte[]>>();

        public IDisposable Subscribe(IPublisher<byte[]> publisher)
        {
            if (!_publishers.Contains(publisher))
                _publishers.Add(publisher);
            return new Unsubscriber<IPublisher<byte[]>>(_publishers, publisher);
        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _localCancelSource.Cancel();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MeshSocketServer()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }

        public void OnPublish(byte[] value)
        {
            var m = new T();
            m.Decode(value);
            foreach (var observer in _observers)
                observer.OnNext(m);
        }
        #endregion
    }
}
