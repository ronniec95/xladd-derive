using System;
using AARC.Mesh.Interface;

namespace AARC.MeshTests
{
    public class MockMonitor : IMonitor
    {
        public MockMonitor()
        {
        }

        public Uri URI { get; set; }

        public void OnError(Exception ex, string channel)
        {
            
        }

        public void OnInfo(string message, string channel)
        {

        }

        public void OnNext(byte[] bytes)
        {

        }
    }
}