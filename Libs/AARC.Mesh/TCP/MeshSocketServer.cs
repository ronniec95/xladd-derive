using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    /// <summary>
    /// A socket server that creates socketservice classes when client services connect.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MeshSocketServer<T> : IMeshTransport<T> where T: IMeshMessage,new()
    {
        private readonly CancellationTokenSource _localCancelSource;
        private readonly CancellationToken _localct;

        private readonly ManualResetEvent _listenAcceptEvent;

        private readonly ConcurrentDictionary<string, SocketService> _connectedRoutes;

        private readonly ILogger _logger;

        private readonly SocketServiceFactory _socketServiceFactory;

        public int MonitorPeriod { get; set; }

        public Action<T> Subscribe { get; set; }

        private ServiceHost _serviceHost;

        public string TransportId { get { return _serviceHost?.ToString(); } }

        public MeshSocketServer(ILogger<MeshSocketServer<T>> logger, SocketServiceFactory socketServiceFactory)
        {
            _localCancelSource = new CancellationTokenSource();
            _listenAcceptEvent = new ManualResetEvent(false);
            _logger = logger;
            _socketServiceFactory = socketServiceFactory;
            _connectedRoutes = new ConcurrentDictionary<string, SocketService>();
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


        public void ServiceConnect(string serverDetails, CancellationToken cancellationToken)
        {
            var details = serverDetails.Split(':');
            var address = details?[0];
            var port = int.Parse(details?[1]);

            if (_connectedRoutes.ContainsKey(serverDetails))
            {
                var service = _connectedRoutes[serverDetails];
                if (!service.ConnectionAlive())
                {
                    service.ManageConnection(address, port, true);
                }
            }
            else
            {
                var service = _socketServiceFactory.Create();
                service.ManageConnection(address, port);
                _connectedRoutes[serverDetails] = service;
            }
        }

        public void Listen(int port, CancellationToken cancellationToken)
        {
            _serviceHost = new ServiceHost { HostName = Dns.GetHostName(), Port = port };
            var ipAddress = SocketHelper.GetLocalHost();
            var localEndPoint = new IPEndPoint(IPAddress.Any, port);
            try
            {
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localct, cancellationToken))
                {
                    var listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    _logger?.LogDebug("Created Listener");
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
        /// Publish Message to service connected under transportId key
        /// </summary>
        /// <param name="transportId">server ip and port</param>
        /// <param name="message"></param>
        public void Publisher(string transportId, T message)
        {
            if (_connectedRoutes.ContainsKey(transportId))
            {
                var bytes = message.Encode();
                if (!_connectedRoutes[transportId].Send(bytes))
                    _logger?.LogWarning($"[transportId] Sending Failed");
            }
            else
            {
                _logger.LogInformation($"{transportId}: NO ROUTES available");
            }
        }

        private void NewConnectionCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.  
            _listenAcceptEvent.Set();
            // Get the socket that handles the client request.  
            Socket listener = (Socket)ar.AsyncState;
            var socket = listener.EndAccept(ar);

            // Create the state object.  
            var service = _socketServiceFactory.Create(socket);
            service.NewMessageBytes += ProcessNewBytes;
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _logger?.LogInformation($"[{service.TransportId}]: New Connection");
            service.ReadAsync();
        }


        /// <summary>
        /// We receive a MeshMesage in bytes.
        /// We should initially get a MessageMessage to Register this service
        /// Action = Register, PayLoad = array of methods actions
        /// </summary>
        /// <param name="bytes"></param>
        private void ProcessNewBytes(string socketDetails, byte[] bytes)
        {
            var message = new T();
            message.Decode(bytes);
            // ToDo - How to check the message was decoded
 //           if (message =)
                Subscribe?.Invoke(message);
        }


        /// <summary>
        /// Check if service is a live and drop if not
        /// </summary>
        /// <param name="socketService">connected to a network service</param>
        protected bool DropClosedConnections(SocketService socketService)
        {
            try
            {
                // Is socket still connected
                if (!socketService.ConnectionAlive())
                {
                    socketService.Shutdown();
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"MSS UNKNOWN ERROR");
            }
            return false;
        }

        protected void ShutdownConnection(SocketService socketService)
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
                    _logger?.LogDebug($"MSS MonitoringSockets connections[{_connectedRoutes.Count}]");
                    foreach (var kvp in _connectedRoutes)
                    {
                        var service = kvp.Value;
                        if (DropClosedConnections(service))
                        {
                            SocketService ss;
                            if (_connectedRoutes.TryRemove(kvp.Key, out ss))
                            {
                                _logger?.LogInformation($"MSS dropped connections[{ss.TransportId}]");
                            }
                            else
                                _logger?.LogInformation($"MSS failed to drop [{service.TransportId}]");
                        }

                    }

                    Task.Delay(MonitorPeriod, _localct).Wait();
                }
            });
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
