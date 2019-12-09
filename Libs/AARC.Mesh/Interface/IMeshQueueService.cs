using System;
namespace AARC.Mesh.Interface
{
    public interface IMeshChannelService: IDuplex<byte[]>, IDisposable
    {
        bool Connected { get; }

        /// <summary>
        /// Connected and ConnectionAlive are more to do with remote services
        /// and detecting stale connections.
        /// This is a particular function using a realtime check for sockets
        /// </summary>
        /// <returns>True if connection is alive</returns>
        bool ConnectionAlive();

        //Action<string, byte[]> NewMessageBytes { get; set; }

        /// <summary>
        /// Start Q service async reads
        /// </summary>
        void ReadAsync();

        string ServiceDetails { get; }
    }
}
