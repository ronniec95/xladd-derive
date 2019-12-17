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

        private readonly MeshChannelProxy<T> _channelProxy;

        public MeshObserver(MeshChannelProxy<T> external)
        {
            _channelProxy = external;
        }

        public MeshObserver(string channelName): this(new MeshChannelProxy<T>(outputChannelName: channelName)) { }

        public MeshChannelResult<MeshMessage> PublishChannel { get { return _channelProxy.PublishChannel; } set { _channelProxy.PublishChannel += value; } }

        public void OnCompleted() => _channelProxy.OnCompleted();

        public void OnError(Exception error) => _channelProxy.OnError(error);

        public void OnNext(T value) => _channelProxy.OnPost(value);

        public void Subscriber(IObservable<MeshMessage> provider)
        {
//            if (provider != null)
//                unsubscriber = provider.Subscribe(this);
        }

        public void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputQChannels) { }

        public void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels) => _channelProxy.RegistePublisherChannels(outputChannels);
    }
}
