using System.Collections.Generic;

namespace AARC.Mesh.Interface
{
    public interface IMeshMessage
    {
        IEnumerable<string> Routes { get; }
        byte[] Encode(byte msgType);

        IMeshMessage Decode(byte[] bytes);
    }
}
