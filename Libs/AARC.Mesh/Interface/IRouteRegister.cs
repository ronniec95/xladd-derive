using System.Collections.Generic;
using AARC.Mesh.Model;

namespace AARC.Mesh.Interface
{
    public interface IRouteRegister<T> where T: class
    {
        /// <summary>
        /// Names of the input queues we wish to subscribe to.
        /// </summary>
        IList<string> InputChannelNames { get; }
        /// <summary>
        /// Names of the output queues we wish to publish to.
        /// </summary>
        IList<string> OutputChannelNames { get; }

        void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputQChannels);

        void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels);

        MeshChannelResult<T> PublishChannel { get; set; }
    }

    public delegate void MeshChannelResult<T>(string action, T message) where T : class;
}