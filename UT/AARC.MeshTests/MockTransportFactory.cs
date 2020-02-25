using System;
using System.Threading.Channels;
using AARC.Mesh.Interface;

namespace AARC.MeshTests
{
    public class MockTransportFactory : IMeshTransportFactory
    {
        public MockTransportFactory()
        {
        }

        public IMeshServiceTransport Create()
        {
            throw new NotImplementedException();
        }


        public IMeshServiceTransport Create(IDisposable dispose)
        {
            throw new NotImplementedException();
        }

        public IMeshServiceTransport Create(string url, ChannelWriter<byte[]> channelWriter)
        {
            throw new NotImplementedException();
        }

        public IMeshServiceTransport Create(Uri URI, ChannelWriter<byte[]> channelWriter)
        {
            throw new NotImplementedException();
        }

        public IMeshServiceTransport Create(IDisposable dispose, ChannelWriter<byte[]> channelWriter)
        {
            throw new NotImplementedException();
        }
    }
}
