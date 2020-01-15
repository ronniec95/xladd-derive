using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh.Model
{
    /// <summary>
    /// A publisher of T as in IObserver<typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MeshObserver<T> : IMeshObserver<T>
    {
        public Action<string> OnConnect { get { return _channelProxy?.OnConnect; } set { _channelProxy.OnConnect = value; } }

        public IList<string> InputChannelNames { get { return _channelProxy.InputChannelNames; } }

        public IList<string> OutputChannelNames { get { return _channelProxy.OutputChannelNames; } }

        private readonly MeshChannelProxy<T> _channelProxy;

        public MeshObserver(MeshChannelProxy<T> external)
        {
            _channelProxy = external;
        }

        public MeshObserver(string channelName): this(new MeshChannelProxy<T>(outputChannelName: channelName)) { }

        public MeshChannelResult<MeshMessage> PublishChannel { get { return _channelProxy.PublishChannel; } set { _channelProxy.PublishChannel += value; } }

        public void OnCompleted() => _channelProxy.OnCompleted();

        public void OnError(Exception error) => _channelProxy.OnError(error);
        // To Transport
        public void OnNext(T value) => _channelProxy.OnPost(value);

        public void OnNext(T value, string transportUrl) => _channelProxy.OnPost(value, transportUrl);

        public void Subscriber() => throw new NotSupportedException("Observers cannot subscribe");

        public void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputQChannels) { }

        public void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels) => _channelProxy.RegistePublisherChannels(outputChannels);
    }
}
