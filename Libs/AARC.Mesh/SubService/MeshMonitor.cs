using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.SubService
{
    public class  MeshMonitor : IMonitor,  IDisposable
    {
        private readonly Channel<byte[]> _sendMessageChannel;
        private readonly Channel<byte[]> _receiveMessageChannel;
        private readonly ChannelWriter<byte[]> _byteWriter;
        private readonly ILogger<MeshMonitor> _logger;
        private readonly CancellationTokenSource _localCancelSource;
        private readonly Uri _uri;
        private readonly IMeshTransportFactory _chServiceFactory;
        private IMeshServiceTransport _transportService = null;

        public Uri URI { get; set; }
        public Task MessageRelay { get; }
        public Task MonitorReceive { get; }


        public MeshMonitor(Channel<byte[]> sendMessageChannel, MeshConfig configuration, IMeshTransportFactory transportFactory, ILogger<MeshMonitor> logger)
        {
            _localCancelSource = new CancellationTokenSource();
            _sendMessageChannel = sendMessageChannel;
            _receiveMessageChannel = Channel.CreateUnbounded<byte[]>();
            _byteWriter = _sendMessageChannel.Writer;
            _chServiceFactory = transportFactory;
            _logger = logger;
            _uri = configuration.SmartMonitor;

            URI = new Uri($"tcp://{MeshUtilities.GetLocalHostFQDN()}");
            _logger.LogDebug(URI.ToString());

            MessageRelay = MeshChannelReader.ReadTask(_sendMessageChannel.Reader, OnPublish, _logger, _localCancelSource.Token);
            MonitorReceive = MeshChannelReader.ReadTask(_receiveMessageChannel.Reader, OnReceive, _logger, _localCancelSource.Token);
        }

        private void OnReceive(byte[] bytes)
        {
            _logger.LogDebug($"MON Got a message {bytes}");
        }

        public void OnInfo(string message, string channel = @"INFO")
        {
            var m = new MeshMessage
            {
                Service = URI,
                EncodingType = 0,
                GraphId = 0,
                Channel = channel,
                XId = MeshUtilities.NewXId,
                State = MeshMessage.States.INFO,
                PayLoad = message
            };

            try
            {
                var bytes = m.Encode(0);
                _logger.LogDebug($"Smart Monitor sending {m.State} ({m.DateTimeTotalSeconds},{m.DateTimeMS}) bytes [{bytes.Length}]");

                OnNext(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Smart Monitor Info send error");
            }
        }

        public void OnError(Exception ex, string channel = @"ERROR")
        {
            var m = new MeshMessage
            {
                Service = URI,
                EncodingType = 0,
                GraphId = 0,
                Channel = channel,
                XId = MeshUtilities.NewXId,
                State = MeshMessage.States.ERROR,
                PayLoad = ex.StackTrace
            };

            try
            {
                var bytes = m.Encode(0);
                _logger.LogDebug($"Smart Monitor sending {m.State} ({m.DateTimeTotalSeconds},{m.DateTimeMS}) bytes [{bytes.Length}]");

                OnNext(bytes);
            }
            catch(Exception sex)
            {
                _logger.LogError(sex, $"Smart Monitor error when sending {ex.Message}");
            }
        }

        public void OnNext(byte[] value) => _byteWriter.WriteAsync(value, _localCancelSource.Token);

        protected void OnPublish(byte[] bytes)
        {
            try
            {
                if (_transportService == null)
                {
                    _transportService = _chServiceFactory.Create(_uri, _receiveMessageChannel.Writer);
                }

                if (_transportService.Connected)
                {
                    _logger.LogDebug($"Sending {_uri} [{bytes.Length}]");
                    MeshUtilities.UpdateNT(DateTime.Now, bytes);
                    _transportService.SenderChannel.WriteAsync(bytes);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monitor Service error; Resetting");
                _transportService?.Dispose();
                _transportService = null;
                throw ex;
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
