using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    using AARC.Mesh.Interface;
    using AARC.Mesh.Model;

    /// <summary>
    /// An asynchronous client socket does not suspend the application while waiting for network operations to complete.
    /// Instead, it uses the standard .NET Framework asynchronous programming model to process the network connection on one thread while the application continues to run on the original thread.
    /// Asynchronous sockets are appropriate for applications that make heavy use of the network or that cannot wait for network operations to complete before continuing.
    /// IObservable to PacketProtcol for socket inputs
    /// IObserver 
    /// </summary>
    public class SocketTransport : SubscriberPattern<byte[]>, IMeshServiceTransport
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        public const int PacketSize = 4194304;

        private CancellationTokenSource _localCancelSource;

        // Client  socket.  
        protected Socket _socket { get; private set; }

        private Uri _url;

        // Receive buffer.  
        private readonly byte[] _rawReceiveBuffer;
        private readonly ILogger _logger;
        private readonly PacketProtocol _packetizer;
        private readonly BufferBlock<byte[]> _byteBlocks;
        /// <summary>
        /// Action Delegate for New Message
        /// </summary>
        //public Action<string, byte[]> NewMessageBytes { get; set; }

        public string Url { get { return _url.ToString(); } }

        public SocketTransport(ILogger logger = null)
        {
            _logger = logger;
            _localCancelSource = new CancellationTokenSource();
            _rawReceiveBuffer = new byte[BufferSize];
            _byteBlocks = new BufferBlock<byte[]>();
            _url = NetworkExt.GetHostNameUrl();

            _packetizer = new PacketProtocol(PacketSize);

            // Pass the assembled bytes message to the IMeshObservers (subscriber)
            _packetizer.MessageArrived += (bytes) =>
            {
                foreach (var observer in _publishers)
                    observer.OnPublish(bytes);
            };
        }

        //public SocketTransport(IServiceProvider serviceProvider) : this(serviceProvider.GetService<ILogger<SocketTransport>>()) { }

        public SocketTransport(Socket socket, ILogger logger = null) : this(logger)
        {
            _socket = socket;
            _url = socket?.GetServiceHost();
        }

        /// <summary>
        /// Checkes to see if the connection is still alive and reconnects.
        /// Connects if not connected before
        /// </summary>
        /// <param name="url"></param>
        /// <param name="reconnect"></param>
        public void ManageConnection(Uri url, bool reconnect = false)
        {
            if (_socket == null)
            {
                _logger?.LogInformation($"[{url}] Connecting");
                _socket = Connect(url);
                _localCancelSource = new CancellationTokenSource();
            }

            if (!_socket.Connected && reconnect)
            {
                _logger?.LogInformation($"[{url}] Reconnecting");
                _socket?.Dispose();
                _socket = Connect(url);
            }
            if (_socket.Connected)
            {
                _url = new Uri(url.AbsoluteUri);
            }
        }

        /// <summary>
        ///
        /// http://tldp.org/HOWTO/TCP-Keepalive-HOWTO/overview.html
        /// </summary>
        /// <param name="hostname"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        protected Socket Connect(string hostname, int port)
        {
            var ipaddress = NetworkExt.GetHostIPAddress(hostname);
            var ipEndPoint = new IPEndPoint(ipaddress, port);
            var worksocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var dsConnectEvent = new ManualResetEvent(false);
            dsConnectEvent.Reset();
            worksocket.BeginConnect(hostname, port, ar =>
            {
                try
                {
                    var s = (Socket)ar.AsyncState;
                    s.EndConnect(ar);
                    s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
                    _logger?.LogInformation($"[{s.TransportId()}]: Connected");
                }
                catch (SocketException se)
                {
                    _logger?.LogInformation($"SS {se.Message}");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, $"SS {ex.Message}");
                }
                finally
                {
                    dsConnectEvent.Set();
                }
            }, worksocket);
            dsConnectEvent.WaitOne();
            return worksocket;
        }

        protected Socket Connect(Uri url) => Connect(url.Host, url.Port);

        public bool Connected { get { return _socket?.Connected ?? false; } }
        public byte[] Receive(CancellationToken token) => _byteBlocks.Receive(token);

        //private void PacketNewMessageBytes(byte[] bytes) => NewMessageBytes?.Invoke(TransportId.ToString(), bytes);

        /// <summary>
        /// We read message packets from the connected subscriber and forward them to MeshSocketServer to
        /// decode them.
        /// </summary>
        public void ReadAsync()
        {
            if ((_localCancelSource?.IsCancellationRequested ?? true) || (!_socket?.Connected ?? true))
                // Need to signal socket is dead if not cancelled
                _logger?.LogInformation($"[{_url}]: ReadAsync cancelled or not connected");
            else
            {
                var result = _socket?.BeginReceive(_rawReceiveBuffer, 0, SocketTransport.BufferSize, 0, new AsyncCallback(ReadCallback), this);
            }

        }

        public void Shutdown()
        {
            // ToDo : Cancellation token
            _localCancelSource.Cancel();
            _logger?.LogInformation($"[{_url}]: Closing Socket");
            if (_socket?.Connected ?? false)
            {
                _socket?.Shutdown(SocketShutdown.Both);
                _socket?.Close();
            }
            _socket = null;
        }

        /// <summary>
        /// As this is a network stream we do not know if our message is complete.
        /// We use packetizer to reassumble
        /// </summary>
        /// <param name="ar">Our ServiceSock object</param>
        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                var service = (SocketTransport)ar.AsyncState;
                var handler = service._socket;

                // Read data from the client socket.   
                int bytesRead = handler?.EndReceive(ar) ?? 0;

                if (bytesRead > 0)
                {
                    //                    _logger?.LogInformation($"[{SocketDetails}]: Rx {bytesRead} bytes");
                    // Clone the buffer to the size we want.
                    // Read
                    var rawmessage = service._rawReceiveBuffer.CloneReduce(bytesRead);
                    _packetizer.DataReceived(rawmessage, bytesRead);
                }
                ReadAsync();
            }
            catch (SocketException se)
            {
                _logger?.LogError(se, "Connection Read error");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, "General Socket Read error");
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                var handler = (Socket)ar.AsyncState;
                // Complete sending the data to the remote device.  
                int bytesSent = handler?.EndSend(ar) ?? 0;
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"[{_socket.TransportId()}]: Failed to Send");
            }
        }

        public bool ConnectionAlive() => !(_socket.Poll(5000, SelectMode.SelectRead) && _socket.Available == 0);

        #region IObserver Support
        public void OnCompleted()
        {
            Dispose();
        }

        public void OnError(Exception error)
        {
            _logger.LogError(error, "Send error");
        }

        /// <summary>
        /// value is a message encoded in bytes that needs to be wrapped in a length
        /// and sent to the listener on the socket
        /// </summary>
        /// <param name="value"></param>
        public void OnPublish(byte[] value)
        {
            try
            {
                var message = PacketProtocol.WrapMessage(value);
                if (_socket?.Connected ?? false)
                {
                    _socket?.BeginSend(message, 0, message.Length, 0, new AsyncCallback(SendCallback), _socket);
                }
                else
                {
                    OnError(new SocketException((int)SocketError.NotConnected));
                    OnCompleted();
                }
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
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
                    Shutdown();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~SocketService()
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
