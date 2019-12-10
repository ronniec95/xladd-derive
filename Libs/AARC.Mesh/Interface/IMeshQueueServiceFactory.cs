using System;

namespace AARC.Mesh.Interface
{
    public interface IMeshTransportFactory
    {
        /// <summary>
        /// The instance should be created with a factory to determine
        /// what kind of Q service this is.
        /// </summary>
        /// <returns></returns>
        IMeshServiceTransport Create();

        /// <summary>
        /// Uses servicedetails work work out how to setup this MeshQueueService
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        IMeshServiceTransport Create(string url);

        /// <summary>
        /// Bit of a hack to allow a socket to be passed in to create Q service
        /// </summary>
        /// <param name="dispose"></param>
        /// <returns></returns>
        IMeshServiceTransport Create(IDisposable dispose);
    }
}
