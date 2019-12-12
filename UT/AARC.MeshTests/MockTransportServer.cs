using System;
using AARC.Mesh.Model;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace AARC.MeshTests
{

    class MockTransportServer : ObserverablePattern<MeshMessage>, IMeshTransport<MeshMessage>, IDuplex<byte[]>
    {
        private readonly ConcurrentDictionary<string, IMeshServiceTransport> _meshServices = new ConcurrentDictionary<string, IMeshServiceTransport>();

        public MockTransportServer()
        {

        }

        public string Url => "localhost:0";

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
            var bytes = value.Encode();
            foreach (var transportId in value.Routes)
                if (_meshServices.ContainsKey(transportId))
                {
                    var service = _meshServices[transportId];
                    if (service.Connected)
                        _meshServices[transportId].OnPublish(bytes);
                    else
                    {
                        _meshServices.Remove(transportId, out service);
                    }
                }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="service"></param>
        public void RegisterService(IMeshServiceTransport service)
        {
            if (!_meshServices.ContainsKey(service.Url))
                _meshServices[service.Url] = service;

            service.Subscribe(this);
            Subscribe(service);
        }

        public void OnPublish(byte[] value)
        {
            var m = new MeshMessage();
            m.Decode(value);
            foreach (var o in _observers)
                o.OnNext(m);
        }

        public void ServiceConnect(string serverDetails, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
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
