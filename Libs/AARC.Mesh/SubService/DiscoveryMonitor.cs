using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AARC.Mesh.Interface;
using System.Threading.Channels;

namespace AARC.Mesh.SubService
{
    public class DiscoveryMonitor<T> : IPublisher<byte[]>, IDisposable where T : IMeshMessage, new()
    {
        private readonly CancellationTokenSource _localCancelSource;
        private readonly ILogger _logger;
        private readonly IMeshTransportFactory _qServiceFactory;
        private readonly Channel<byte[]> _parentReceiver;
        private readonly Task ChannelReceiverProcessor;

        private IMeshServiceTransport _discoveryService;

        public Action<T> DiscoveryReceiveMessage { get; set; }

        public Action<T, string> DiscoverySendMessage { get; set; }

        public Action<T, string, string> DiscoveryErrorMessage { get; set; }

        public DiscoveryMonitor(ILogger<DiscoveryMonitor<T>> logger, IMeshTransportFactory qServiceFactory)
        {
            _localCancelSource = new CancellationTokenSource();
            _logger = logger;
            _qServiceFactory = qServiceFactory;
            _parentReceiver = Channel.CreateUnbounded<byte[]>();
            ChannelReceiverProcessor = Task.Factory.StartNew(async () =>
            {
                var reader = _parentReceiver.Reader;
                try
                {
                    while (!_localCancelSource.IsCancellationRequested)
                    {
                        var bytes = await reader.ReadAsync(_localCancelSource.Token);
                        OnPublish(bytes);
                    }
                }
                finally
                {
                    _logger.LogInformation("Parent Reader complete");
                }
            });
        }

        public async Task StartListeningServices(Uri discoveryUrl, CancellationToken cancellationToken)
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
                                _discoveryService = _qServiceFactory.Create(discoveryUrl);
                                _discoveryService.ReceiverChannel = _parentReceiver.Writer;
                            }

                            if (_discoveryService.Connected)
                            {
                                var message = new T();
                                DiscoverySendMessage.Invoke(message, serviceUrl);

                                OnSend(message);
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
            DiscoveryReceiveMessage?.Invoke(message);
        }

        public void OnError(string errorMessage, string url)
        {
            var message = new T();
            DiscoveryErrorMessage?.Invoke(message, url, errorMessage);
            OnSend(message);
        }
        public void OnSend(T message)
        {
            var obytes = message.Encode();
            // Todo: Not sure I like this
            _discoveryService.SenderChannel.WriteAsync(obytes);
            _logger?.LogDebug($"DS Tx {obytes.Length}");
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
                    Task.WaitAll(ChannelReceiverProcessor);
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
