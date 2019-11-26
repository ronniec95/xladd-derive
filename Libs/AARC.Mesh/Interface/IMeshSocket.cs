using System.Net.Sockets;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.Interface
{
    public interface IMeshSocket
    {
        void SttListener(Socket socket, CancellationToken token, ILogger logger);
    }
}
