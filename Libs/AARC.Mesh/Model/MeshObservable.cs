using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh.Model
{
    /// <summary>
    /// A mesh subscriber to T as in IObservable<typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MeshObservable<T> : IMeshObservable<T> where T : class, new()
    {
        public IList<string> InputChannelNames { get; set; }

        /// <summary>
        /// Names of Output Queues
        /// </summary>
        public IList<string> OutputChannelNames { get; set; }

        private MeshChannelProxy<T> _channelsMarshal;

        public MeshObservable(MeshChannelProxy<T> external)
        {
            _channelsMarshal = external;
        }

        public MeshObservable(string qname) : this(new MeshChannelProxy<T>(inputq: qname)) { }

        public IDisposable Subscribe(IObserver<T> observer) => _channelsMarshal.Subscribe(observer);

        public void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputQChannels) => _channelsMarshal.RegisterReceiverChannels(inputQChannels);

        public void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels) {}

        public MeshChannelResult<MeshMessage> PublishChannel { get { return _channelsMarshal.PublishChannel; } set { _channelsMarshal.PublishChannel += value; } }
    }
}
