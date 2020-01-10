using System;

namespace AARC.Mesh.Interface
{
    public interface IChannelProxy
    {
        Action<string> OnConnect { get; set; }
    }
}