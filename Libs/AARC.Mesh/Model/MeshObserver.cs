using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh.Model
{
    /// <summary>
    /// A publisher of T as in IObserver<typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MeshObserver<T> : IMeshObserver<T> where T : class
    {
        private IDisposable unsubscriber;

        public IList<string> InputChannelNames { get; set; }

        /// <summary>
        /// Names of Output Queues
        /// </summary>
        public IList<string> OutputChannelNames { get; set; }

        private readonly MeshChannelProxy<T> _channelsMarshal;

        public MeshObserver(MeshChannelProxy<T> external)
        {
            _channelsMarshal = external;
        }

        public MeshObserver(string qname): this(new MeshChannelProxy<T>(outputq: qname)) { }

        public MeshChannelResult<MeshMessage> PublishChannel { get { return _channelsMarshal.PublishChannel; } set { _channelsMarshal.PublishChannel += value; } }

        public void OnCompleted() => _channelsMarshal.OnCompleted();

        public void OnError(Exception error) => _channelsMarshal.OnError(error);

        public void OnNext(T value) => _channelsMarshal.OnPost(value);

        public void Subscriber(IObservable<MeshMessage> provider)
        {
//            if (provider != null)
//                unsubscriber = provider.Subscribe(this);
        }

        public void RegisterDependencies(MeshDictionary<MeshMessage> outputQs) => _channelsMarshal.RegisterDependencies(outputQs: outputQs);

        public void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputQChannels) { }

        public void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels) => _channelsMarshal.RegistePublisherChannels(outputChannels);
    }
}
