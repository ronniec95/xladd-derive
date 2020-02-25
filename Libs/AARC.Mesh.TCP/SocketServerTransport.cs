using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AARC.Mesh.Interface;
using AARC.Mesh.SubService;
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
        private readonly Channel<byte[]> _parentReceiver;
        private readonly ManualResetEvent _listenAcceptEvent;

        private readonly ConcurrentDictionary<Uri, IMeshServiceTransport> _meshServices;

        private readonly ILogger _logger;

        private readonly IMeshTransportFactory _qServiceFactory;

        public int MonitorPeriod { get; set; }

        public Uri URI { get;  set; }

        private Task ChannelReceiverProcessor;
        private Task MonitorServices;

        private IMonitor _monitor;

        public SocketServerTransport(IMonitor monitor, ILogger<SocketServerTransport<T>> logger, IMeshTransportFactory qServiceFactory)
        {
            _localCancelSource = new CancellationTokenSource();
            _listenAcceptEvent = new ManualResetEvent(false);
            _logger = logger;
            _qServiceFactory = qServiceFactory;
            _parentReceiver = Channel.CreateUnbounded<byte[]>();
            _meshServices = new ConcurrentDictionary<Uri, IMeshServiceTransport>();
            MonitorPeriod = 15000;
            _localct = _localCancelSource.Token;
            _monitor = monitor;

            ChannelReceiverProcessor = MeshChannelReader.ReadTask(_parentReceiver.Reader, OnPublish, _logger, _localCancelSource.Token);
        }

        public async Task StartListeningServices(CancellationToken cancellationToken) => await Task.Factory.StartNew(() => Listen(cancellationToken));

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
        /// <returns>new connection && connected</returns>
        public bool ServiceConnect(Uri servicedetails, CancellationToken cancellationToken)
        {
            if (_meshServices.ContainsKey(servicedetails))
            {
                var service = _meshServices[servicedetails];
                if (service.ConnectionAlive())
                    // Not a new connection
                    return false;

                if (_meshServices.TryRemove(servicedetails, out service))
                    service.Dispose();
            }

            _logger.LogInformation($"Creating a connecting to {servicedetails}");
            var qss = _qServiceFactory.Create(servicedetails, _parentReceiver.Writer);
            _meshServices[servicedetails] = qss;
            return qss.Connected;
        }

        public void SetPort(int port) => URI = NetworkExt.GetHostNameUrl(port);

        public void Listen(CancellationToken cancellationToken)
        {
            var ipAddress = NetworkExt.GetHostIPAddress(URI.Host);
            var localEndPoint = new IPEndPoint(IPAddress.Any, URI.Port);
            try
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localct, cancellationToken))
                {
                    var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _logger?.LogDebug($"Created Listener on {URI}");
                    // Bind the socket to the local endpoint and   
                    // listen for incoming connections.  
                    listener.Bind(localEndPoint);
                    listener.Listen(10);
                    MonitorServices = MonitorStaleConnectedServices();
                    // Start an asynchronous socket to listen for connections.
                    while (!linkedCts.IsCancellationRequested)
                    {
                        _listenAcceptEvent.Reset();
                        _logger?.LogInformation($"[{ipAddress}:{URI.Port}] Accepting connections...");
                        listener.BeginAccept(new AsyncCallback(NewConnectionCallback), listener);
                        _listenAcceptEvent.WaitOne();
                    }
                }
            }
            catch (Exception e)
            {
                _monitor.OnError(e, "ERROR");
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
            var service = _qServiceFactory.Create(socket, _parentReceiver.Writer);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            var status = $"[{service.URI}]: New Connection";
            _logger?.LogInformation(status);
            //service.ReceiverChannel = _parentReceiver.Writer;
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
                _monitor.OnError(e, "ERROR");
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
                    foreach (var kvp in _meshServices)
                    {
                        var service = kvp.Value;
                        _logger?.LogDebug($"SS Checking Connection to {service.URI}");
                        if (DropClosedConnections(service))
                        {
                            if (_meshServices.TryRemove(kvp.Key, out IMeshServiceTransport ss))
                            {
                                var status = $"SS dropped connections[{ss.URI}]";
                                _monitor.OnInfo(status, "STATUS");
                                _logger?.LogInformation(status);
                            }
                            else
                                _logger?.LogInformation($"SS failed to drop [{service.URI}]");
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

        public void OnError(Exception error) =>_monitor.OnError(error, "ERROR");

        public void OnPublish(byte[] value)
        {
            _monitor.OnNext(value);
            var m = new T();
            m.Decode(value);
            foreach (var observer in _observers)
                observer.OnNext(m);
        }

        public void OnNext(T value)
        {
            // Todo: Need something to allow encoding switching
            var bytes = value.Encode(0);
            _monitor.OnNext(bytes);
            foreach (var transportId in value.Routes)
                if (_meshServices.ContainsKey(transportId))
                {
                    var service = _meshServices[transportId];
                    if (service.Connected)
                        _meshServices[transportId].SenderChannel.WriteAsync(bytes);
                    else
                    {
                        // Todo: What if we cant remove?
                        var status = $"{transportId}: Disconnected - Removing";
                        _logger.LogInformation(status);
                        if (_meshServices.TryRemove(transportId, out service))
                            service.Dispose();
                    }
                }
                else
                    _logger.LogInformation($"{transportId}: NO ROUTES available");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _localCancelSource.Cancel();
                    Task.WaitAll(ChannelReceiverProcessor, MonitorServices);
                    ChannelReceiverProcessor = Task.CompletedTask;
                    MonitorServices = Task.CompletedTask;
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
        #endregion
    }
}
