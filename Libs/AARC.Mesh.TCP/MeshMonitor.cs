using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.TCP
{
    public class  MeshMonitor : IDisposable
    {
        private readonly Channel<byte[]> _messageChannel;
        private readonly ChannelWriter<byte[]> _byteWriter;
        private readonly ILogger<MeshMonitor> _logger;
        private readonly CancellationTokenSource _localCancelSource;
        private readonly Uri _transport;

        public Task MessageRelay { get; }

        public MeshMonitor(ILogger<MeshMonitor> logger, Uri transportId)
        {
            _localCancelSource = new CancellationTokenSource();
            _messageChannel = Channel.CreateUnbounded<byte[]>();
            _byteWriter = _messageChannel.Writer;
            _logger = logger;
            _transport = transportId;

            MessageRelay = Task.Factory.StartNew(async () =>
            {
                var reader = _messageChannel.Reader;
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
                    _logger.LogInformation("Monitor Complete");
                }
            });
        }

        public void OnNext(byte[] value) => _byteWriter.WriteAsync(value, _localCancelSource.Token);

        protected void OnPublish(byte[] value)
        {
            using (var s = new UdpClient(_transport.Host, _transport.Port))
            {
                try
                {
                    s.SendAsync(value, value.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Monitor Service error");
                }
            }
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
        // ~MeshMonitor()
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
