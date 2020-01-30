using System;
namespace AARC.Mesh.Model
{
    public class ChannelMetrics
    {
        public string Name { get; set; }
        public Type ReturnType { get; set; }
        public int Connections { get; set; }
        public int NoMsgReceived { get; set; }
        public int NoMsgSent { get; set; }
        public int Errors { get; set; }
    }
}
