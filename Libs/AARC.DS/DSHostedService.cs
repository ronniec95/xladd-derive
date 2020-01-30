using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AARC.Mesh.SubService;
using AARC.Mesh.Model;

namespace AARC.DS
{
    public class DSHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<DSHostedService> _logger;
        private readonly ServiceHost _discoveryAddress;
        private readonly MeshServiceManager _msm;

        public DSHostedService(ILogger<DSHostedService> logger, MeshServiceManager meshServiceManager, IConfiguration configuration)
        {
            _discoveryAddress = new ServiceHost(configuration.GetValue<string>("ds", "localhost:9999"));
            _msm = meshServiceManager;
            _logger = logger;
        }

        /// <summary>
        /// Create Find DiscoveryService
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation($"Starting {this.GetType().Name}");

            // Connect to Discovery Service
            var t1 = _msm.StartDiscoveryServices(_discoveryAddress, cancellationToken);

            return t1;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return _msm.Cancel();
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _msm.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MeshHostedService()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
