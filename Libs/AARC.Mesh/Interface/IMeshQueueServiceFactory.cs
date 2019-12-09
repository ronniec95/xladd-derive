using System;

namespace AARC.Mesh.Interface
{
    public interface IMeshQueueServiceFactory
    {
        /// <summary>
        /// The instance should be created with a factory to determine
        /// what kind of Q service this is.
        /// </summary>
        /// <returns></returns>
        IMeshChannelService Create();

        /// <summary>
        /// Uses servicedetails work work out how to setup this MeshQueueService
        /// </summary>
        /// <param name="servicedetails"></param>
        /// <returns></returns>
        IMeshChannelService Create(string servicedetails);

        /// <summary>
        /// Bit of a hack to allow a socket to be passed in to create Q service
        /// </summary>
        /// <param name="dispose"></param>
        /// <returns></returns>
        IMeshChannelService Create(IDisposable dispose);
    }
}
