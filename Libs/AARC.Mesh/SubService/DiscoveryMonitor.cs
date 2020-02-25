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
        private readonly byte _msgEncoding;

        private IMeshServiceTransport _transportService;

        public Action<T> DiscoveryReceiveMessage { get; set; }

        public Action<T, string> DiscoverySendMessage { get; set; }

        public Action<T, string, string> DiscoveryErrorMessage { get; set; }
        public Action ResetDiscoveryState { get;  set; }

        public DiscoveryMonitor(ILogger<DiscoveryMonitor<T>> logger, IMeshTransportFactory qServiceFactory)
        {
            _transportService = null;
            _msgEncoding = 0;
            _localCancelSource = new CancellationTokenSource();
            _logger = logger;
            _qServiceFactory = qServiceFactory;
            _parentReceiver = Channel.CreateUnbounded<byte[]>();
            ChannelReceiverProcessor = MeshChannelReader.ReadTask(_parentReceiver.Reader, OnPublish, _logger, _localCancelSource.Token);
        }

        public async Task StartListeningServices(Uri discoveryUrl, CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                var retries = 0;
                using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(_localCancelSource.Token, cancellationToken))
                    while (!linkedCts.IsCancellationRequested && retries < 3)
                        try
                        {
                            var hostname = MeshUtilities.GetLocalHostFQDN();

                            _logger?.LogInformation($"Looking for Discovery Service [{hostname}]");

                            var delay = 1000;

                            do
                            {
                                try
                                {
                                    if (_transportService == null)
                                    {
                                        ResetDiscoveryState.Invoke();
                                        _transportService = _qServiceFactory.Create(discoveryUrl, _parentReceiver.Writer);
                                    }

                                    if (_transportService.Connected)
                                    {
                                        var message = new T();
                                        DiscoverySendMessage.Invoke(message, hostname);

                                        OnSend(message);
                                    }
                                    else
                                    {
                                        // Bad state shutdown services
                                        _transportService.Dispose();
                                        _transportService = null;
                                    }
                                    retries = 0;
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
                        }
                        catch (Exception e)
                        {
                            ++retries;
                            _logger.LogError(e, "DS error");
                        }
                _logger?.LogInformation("DS Exiting");
            }, cancellationToken);
        }

        public void OnPublish(byte[] ibytes)
        {
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

        public void OnError(string errorMessage, Uri url)
        {
            var message = new T();
            DiscoveryErrorMessage?.Invoke(message, url.AbsoluteUri, errorMessage);
            OnSend(message);
        }

        public void OnSend(T message)
        {
            var obytes = message.Encode(_msgEncoding);
            // Todo: Not sure I like this
            _transportService.SenderChannel.WriteAsync(obytes);
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
