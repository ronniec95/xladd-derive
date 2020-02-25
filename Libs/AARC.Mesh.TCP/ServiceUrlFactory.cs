using System;

namespace AARC.Mesh.TCP
{
    public class ServiceUrlFactory
    {
        protected readonly Uri _url;

        public ServiceUrlFactory()
        {
            _url = new Uri($"tcp://{MeshUtilities.GetLocalHostFQDN()}");
        }

        public string TransportId => _url.AbsoluteUri;
    }
}
