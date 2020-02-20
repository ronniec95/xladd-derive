using System;
using AARC.Mesh.Model;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using AARC.Mesh;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace AARC.MeshTests
{

    class MockTransportServer : ObserverablePattern<MeshMessage>, IMeshTransport<MeshMessage>, IDuplex<byte[]>
    {
        private readonly ConcurrentDictionary<Uri, IMeshServiceTransport> _meshServices;

        public int MonitorPeriod { get; }
        private CancellationTokenSource _localCancelSource;
        private ManualResetEvent _listenAcceptEvent;
        private ILogger<MockTransportServer> _logger;
        private IMeshTransportFactory _qServiceFactory;
        private readonly Channel<byte[]> _parentReceiver;
        private CancellationToken _localct;
        protected byte _msgEncoderType;

        public MockTransportServer(ILogger<MockTransportServer> logger, IMeshTransportFactory qServiceFactory)
        {
            _localCancelSource = new CancellationTokenSource();
            _listenAcceptEvent = new ManualResetEvent(false);
            _logger = logger;
            _qServiceFactory = qServiceFactory;
            _parentReceiver = Channel.CreateUnbounded<byte[]>();
            _meshServices = new ConcurrentDictionary<Uri, IMeshServiceTransport>();
            MonitorPeriod = 15000;
            _localct = _localCancelSource.Token;
            _msgEncoderType = 0;
            URI = new Uri("tcp://localhost:0");
        }

        public Uri URI { get; set; }

        public Task Cancel()
        {
            throw new NotImplementedException();
        }

        public void Dispose() { }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(MeshMessage value)
        {
            var bytes = value.Encode(_msgEncoderType);
            foreach (var transportId in value.Routes)
                if (_meshServices.ContainsKey(transportId))
                {
                    var service = _meshServices[transportId];
                    if (service.Connected)
                        _meshServices[transportId].SenderChannel.WriteAsync(bytes);
                    else
                    {
                        _meshServices.Remove(transportId, out service);
                    }
                }
                else throw new EntryPointNotFoundException($"No Route to {transportId}");
        }

        public void OnPublish(byte[] value)
        {
            var m = new MeshMessage();
            m.Decode(value);
            foreach (var o in _observers)
                o.OnNext(m);
        }

        public bool ServiceConnect(Uri servicedetails, CancellationToken cancellationToken)
        {
            if (_meshServices.ContainsKey(servicedetails))
            {
                var service = _meshServices[servicedetails];
                if (service.ConnectionAlive())
                    return false;

                if (_meshServices.TryRemove(servicedetails, out service))
                    service.Dispose();
            }

            _logger.LogInformation($"Creating a connecting to {servicedetails}");
            var qss = _qServiceFactory.Create(servicedetails);
            _meshServices[servicedetails] = qss;
            return qss.Connected;
        }

        public Task StartListeningServices(int port, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        #region Subscriber
        public List<IPublisher<byte[]>> _publishers = new List<IPublisher<byte[]>>();

        public IDisposable Subscribe(IPublisher<byte[]> publisher)
        {
            if (!_publishers.Contains(publisher))
                _publishers.Add(publisher);
            return new Unsubscriber<IPublisher<byte[]>>(_publishers, publisher);
        }
        #endregion
    }

}
