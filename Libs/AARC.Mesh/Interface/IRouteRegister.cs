using System.Collections.Generic;
using AARC.Mesh.Model;

namespace AARC.Mesh.Interface
{
    public interface IRouteRegister<T> where T: class
    {
        /// <summary>
        /// Readable name of the input channels we wish to subscribe to.
        /// </summary>
        string InputChannelAlias { get; }
        /// <summary>
        /// Readable name of the output channels we wish to publish to.
        /// </summary>
        string OutputChannelAlias { get; }

        void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputQChannels);

        void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels);

        MeshChannelResult<T> PublishChannel { get; set; }
    }

    public delegate void MeshChannelResult<T>(string action, T message) where T : class;
}