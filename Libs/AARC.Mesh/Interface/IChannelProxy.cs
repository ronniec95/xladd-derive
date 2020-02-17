using System;

namespace AARC.Mesh.Interface
{
    public interface IChannelProxy
    {
        Action<Uri> OnConnect { get; set; }
    }
}