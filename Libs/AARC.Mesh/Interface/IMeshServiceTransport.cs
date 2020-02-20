using System;
using System.Threading.Channels;

namespace AARC.Mesh.Interface
{
    public interface IMeshServiceTransport: IDisposable
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

        ChannelWriter<byte[]> ReceiverChannel { get; set; }

        ChannelWriter<byte[]> SenderChannel { get; }

        Uri URI { get; }

        void Shutdown();
    }
}
