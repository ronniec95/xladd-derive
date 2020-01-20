using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh.Model
{
    /// <summary>
    /// A mesh subscriber to T as in IObservable<typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MeshObservable<T> : IMeshObservable<T> // where T : class, new()
    {
        public string IputChannelAlias { get { return _channelProxy.IputChannelAlias; } }

        public string OutputChannelAlias { get { return _channelProxy.OutputChannelAlias; } }

        private MeshChannelProxy<T> _channelProxy;

        public MeshObservable(MeshChannelProxy<T> external)
        {
            _channelProxy = external;
        }

        public MeshObservable(string channelName, int clusterType = 0) : this(new MeshChannelProxy<T>(inputChannelName: channelName, clusterType: clusterType)) { }

        public IDisposable Subscribe(IObserver<T> observer) => _channelProxy.Subscribe(observer);

        public void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputQChannels) => _channelProxy.RegisterReceiverChannels(inputQChannels);

        public void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels) {}

        public MeshChannelResult<MeshMessage> PublishChannel { get { return _channelProxy.PublishChannel; } set { _channelProxy.PublishChannel += value; } }
    }
}
