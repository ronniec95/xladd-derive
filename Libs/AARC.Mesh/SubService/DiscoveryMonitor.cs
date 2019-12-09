using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AARC.Mesh.Interface;

namespace AARC.Mesh.SubService
{
    public class DiscoveryMonitor<T> : IPublisher<byte[]>, IDisposable where T : IMeshMessage, new()
    {
        private readonly CancellationTokenSource _localCancelSource;
        private readonly ILogger _logger;
        private readonly IMeshQueueServiceFactory _qServiceFactory;

        private IMeshChannelService _discoveryService;

        public Action<T> DiscoveryReceive { get; set; }

        public Action<T, string> DiscoverySend { get; set; }

        public DiscoveryMonitor(ILogger<DiscoveryMonitor<T>> logger, IMeshQueueServiceFactory qServiceFactory)
        {
            _localCancelSource = new CancellationTokenSource();
            _logger = logger;
            _qServiceFactory = qServiceFactory;
        }

        public async Task StartListeningServices(string serviceDetails, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                _logger?.LogInformation($"Looking for Discovery Service");

                var delay = 1000;

                var serviceUrl = $"tcp://{Dns.GetHostName()}";
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localCancelSource.Token, cancellationToken))
                    do
                    {
                        try
                        {
                            if (_discoveryService == null)
                            {
                                _discoveryService = _qServiceFactory.Create(serviceDetails);
                                _discoveryService.Subscribe(this);
                            }

                            if (_discoveryService.Connected)
                            {
                                var message = new T();
                                DiscoverySend.Invoke(message, serviceUrl);

                                var obytes = message.Encode();
                                _discoveryService.OnPublish(obytes);
                                _logger?.LogDebug($"DS Tx {obytes.Length}");
                            }
                            else
                            {
                                // Bad state shutdown services
                                _discoveryService.Dispose();
                                _discoveryService = null;
                            }
                        }
                        catch (SocketException se)
                        {
                            _logger.LogError(se, $"DS Connection Error: {se.Message}");
                            delay = 1000;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "DS General error");
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

        public void OnPublish(byte[] ibytes)
        {
            _logger?.LogDebug($"DS Rx {ibytes.Length}");
            var message = new T();
            message.Decode(ibytes);
            DiscoveryReceive?.Invoke(message);
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
