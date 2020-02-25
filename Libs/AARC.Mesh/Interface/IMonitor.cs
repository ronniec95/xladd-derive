using System;
namespace AARC.Mesh.Interface
{
    public interface IMonitor
    {
        Uri URI { get; set; }
        void OnNext(byte[] bytes);
        void OnInfo(string message, string channel);
        void OnError(Exception ex, string channel);
    }
}
