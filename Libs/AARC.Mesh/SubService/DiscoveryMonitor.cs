using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AARC.Mesh.Interface;
using AARC.Mesh.TCP;
using System.Net;
using AARC.Mesh.Model;

namespace AARC.Mesh.SubService
{
    public class DiscoveryMonitor<T> : IDisposable where T : IMeshMessage, new()
    {
        private readonly CancellationTokenSource _localCancelSource;
        private readonly ILogger _logger;
        private readonly SocketServiceFactory _socketServiceFactory;

        private SocketService _discoveryService;

        public Action<T> DiscoveryReceive { get; set; }

        public Action<T, string> DiscoverySend { get; set; }

        public DiscoveryMonitor(ILogger<DiscoveryMonitor<T>> logger, SocketServiceFactory socketServiceFactory)
        {
            _localCancelSource = new CancellationTokenSource();
            _logger = logger;
            _socketServiceFactory = socketServiceFactory;
        }

        public async Task StartListeningServices(ServiceHost serviceHost, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                _logger?.LogInformation($"Looking for Discovery Service {serviceHost}");

                var delay = 1000;

                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localCancelSource.Token, cancellationToken))
                    do
                    {
                        try
                        {
                            if (_discoveryService == null)
                            {
                                _discoveryService = _socketServiceFactory.Create();
                                _discoveryService.ManageConnection(serviceHost.HostName, serviceHost.Port, false);
                                _discoveryService.ReadAsync();
                                _discoveryService.NewMessageBytes += ProcessDiscoveryServiceMessage;
                            }

                            if (!_discoveryService.Connected)
                            {
                                // Bad state shutdown services
                                _discoveryService.Shutdown();
                                _discoveryService.Dispose();
                                _discoveryService = null;
                            }
                            else //(_discoveryService.Connected)
                                RegisterDiscoveryService();
                        }
                        catch (SocketException se)
                        {
                            _logger.LogError("DS Connection error", se);
                            delay = 1000;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError("DS General error", ex);
                            delay = 1000;
                        }
                        finally
                        {
                            Task.Delay(delay, linkedCts.Token).Wait();
                        }
                    } while (!linkedCts.IsCancellationRequested);
                _logger?.LogInformation("DS Exiting");
            }, cancellationToken);
        }

        private void ProcessDiscoveryServiceMessage(string arg1, byte[] bytes)
        {
            var message = new T();
            message.Decode(bytes);
            DiscoveryReceive?.Invoke(message);
        }

        public void RegisterDiscoveryService()
        {
            var message = new T();
            DiscoverySend.Invoke(message, Dns.GetHostName());
            
            var bytes = message.Encode();
            _logger?.LogDebug($"DS Tx {bytes.Length}");
            //var bytes = System.Text.Encoding.ASCII.GetBytes(json);
            _discoveryService.Send(bytes);
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
        // ~DiscoveryMonitor()
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
