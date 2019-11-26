using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    using AARC.Mesh.Model;
    /// <summary>
    /// An asynchronous client socket does not suspend the application while waiting for network operations to complete.
    /// Instead, it uses the standard .NET Framework asynchronous programming model to process the network connection on one thread while the application continues to run on the original thread.
    /// Asynchronous sockets are appropriate for applications that make heavy use of the network or that cannot wait for network operations to complete before continuing.
    /// </summary>
    public class SocketService : IDisposable
    {
        // Size of receive buffer.  
        public const int BufferSize = 1024;
        public const int PacketSize = 1024;

        private CancellationTokenSource _localCancelSource;

        // Client  socket.  
        protected Socket _socket { get; private set; }

        public ServiceHost TransportId;

        // Receive buffer.  
        private readonly byte[] _rawReceiveBuffer;
        private readonly ILogger _logger;
        private readonly PacketProtocol _packetizer;
        private readonly BufferBlock<byte[]> _byteBlocks;
        /// <summary>
        /// Action Delegate for New Message
        /// </summary>
        public Action<string, byte[]> NewMessageBytes { get; set; }

        public SocketService(ILogger logger = null)
        {
            _logger = logger;
            _localCancelSource = new CancellationTokenSource();
            _rawReceiveBuffer = new byte[BufferSize];
            _byteBlocks = new BufferBlock<byte[]>();
            _packetizer = new PacketProtocol(PacketSize);
            _packetizer.MessageArrived += PacketNewMessageBytes;
            TransportId = new ServiceHost();
            TransportId.HostName = Dns.GetHostName();
        }

        public SocketService(IServiceProvider serviceProvider) : this(serviceProvider.GetService<ILogger<SocketService>>()) { }

        public SocketService(Socket socket, ILogger logger = null) : this(logger)
        {
            _socket = socket;
            TransportId = socket?.GetServiceHost();
        }

        /// <summary>
        /// Checkes to see if the connection is still alive and reconnects.
        /// Connects if not connected before
        /// </summary>
        /// <param name="serverAddress"></param>
        /// <param name="port"></param>
        /// <param name="reconnect"></param>
        public void ManageConnection(string serverAddress, int port, bool reconnect = false)
        {
            if (_socket == null)
            {
                _logger?.LogInformation($"[{serverAddress}:{port}] Connecting");
                _socket = Connect(serverAddress, port);
                _localCancelSource = new CancellationTokenSource();
            }

            if (!_socket.Connected && reconnect)
            {
                _logger?.LogInformation($"[{serverAddress}:{port}] Reconnecting");
                _socket?.Dispose();
                _socket = Connect(serverAddress, port);
            }
            if (_socket.Connected)
            {
                TransportId = new ServiceHost { HostName = serverAddress, Port = port };
            }
        }

        /// <summary>
        ///
        /// http://tldp.org/HOWTO/TCP-Keepalive-HOWTO/overview.html
        /// </summary>
        /// <param name="serverAddress"></param>
        /// <param name="port"></param>
        /// <returns></returns>
        protected Socket Connect(string serverAddress, int port)
        {
            var ipaddress = SocketHelper.GetHostIPAddress(serverAddress);
            var ipEndPoint = new IPEndPoint(ipaddress, port);
            var worksocket = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            var dsConnectEvent = new ManualResetEvent(false);
            dsConnectEvent.Reset();
            worksocket.BeginConnect(serverAddress, port, ar =>
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

/*        void SetKeepAlive(bool on, uint keepAliveTime, uint keepAliveInterval)
        {
            int size = Marshal.SizeOf(new uint());

            var inOptionValues = new byte[size * 3];

            BitConverter.GetBytes((uint)(on ? 1 : 0)).CopyTo(inOptionValues, 0);
            BitConverter.GetBytes((uint)time).CopyTo(inOptionValues, size);
            BitConverter.GetBytes((uint)interval).CopyTo(inOptionValues, size * 2);

            socket.IOControl(IOControlCode.KeepAliveValues, inOptionValues, null);
        }*/

        public bool Connected {  get { return _socket?.Connected ?? false;  } }
        public byte[] Receive(CancellationToken token) => _byteBlocks.Receive(token);

        /// <summary>
        /// Send bytes Wrapped on socket if connected.
        /// </summary>
        /// <param name="byteData">Message inbytes</param>
        /// <returns>true for send and false if there was an error</returns>
        public bool Send(byte[] byteData)
        {
            try
            {
                var message = PacketProtocol.WrapMessage(byteData);
                if (_socket?.Connected ?? false)
                {
                    _socket?.BeginSend(message, 0, message.Length, 0, new AsyncCallback(SendCallback), _socket);
                    return true;
                }
                _logger?.LogWarning($"[{TransportId}] Send failed - socket not connected");
            }
            catch(Exception ex)
            {
                _logger.LogWarning(ex, "Send error");
            }
            return false;
        }

        private void PacketNewMessageBytes(byte[] bytes) => NewMessageBytes?.Invoke(TransportId.ToString(), bytes);


        public void ReadAsync()
        {
            if (!_localCancelSource?.IsCancellationRequested ?? false)
            {
                var result = _socket?.BeginReceive(_rawReceiveBuffer, 0, SocketService.BufferSize, 0, new AsyncCallback(ReadCallback), this);
            }
            else _logger?.LogInformation($"[{TransportId}]: ReadAsync cancelled");
        }

        public void Shutdown()
        {
            // ToDo : Cancellation token
            _localCancelSource.Cancel();
            _logger?.LogInformation($"[{TransportId}]: Closing Socket");
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
                var service = (SocketService)ar.AsyncState;
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
            catch(SocketException se)
            {
                _logger?.LogError(se, "Connection Read error");
            }
            catch(Exception e)
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
//                _logger?.LogInformation($"[{handler.SocketDetails()}]: Tx {bytesSent} bytes");
            }
            catch (Exception e)
            {
                _logger?.LogError(e, $"[{_socket.TransportId()}]: Failed to Send");
            }
        }

        public bool ConnectionAlive() => !(_socket.Poll(5000, SelectMode.SelectRead) && _socket.Available == 0);

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
