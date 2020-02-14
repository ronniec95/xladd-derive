using System;
using System.Collections.Generic;

namespace AARC.Mesh.Interface
{
    public interface IMeshMessage
    {
        IEnumerable<Uri> Routes { get; }
        byte[] Encode(byte msgType);

        IMeshMessage Decode(byte[] bytes);
    }
}
