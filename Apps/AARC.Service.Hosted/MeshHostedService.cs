
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AARC.Mesh.SubService;
using AARC.Mesh.Model;

namespace AARC.Service.Hosted
{
    public class MeshHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<MeshHostedService> _logger;
        private readonly MeshServiceManager _msm;
        private string[] queueServiceNames;
        private readonly ServiceHost _discoveryAddress;
        private readonly int _listeningPort;


        public MeshHostedService(ILogger<MeshHostedService> logger, MeshServiceManager meshServiceManager, IConfiguration configuration)
        {
            var service = configuration.GetValue<string>("service");
            if (!string.IsNullOrEmpty(service))
                queueServiceNames = new string[] { service };

            _discoveryAddress = new ServiceHost(configuration.GetValue<string>("ds", "localhost:9999"));

            _logger = logger;
            _msm = meshServiceManager;
            // Todo: Bit of a hack as DS should supply port
            _msm.ListeningPort = configuration.GetValue<Int32>("port", 0);
        }

        /// <summary>
        /// Create Find DiscoveryService
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation($"Starting {this.GetType().Name}");
            if (queueServiceNames == null)
            {
                _logger.LogInformation("Missing mesh queue service name - exiting");
                return null;
            }
            foreach (var name in queueServiceNames)
            {
                var meshAction = AARC.Graph.Test.MeshFactory.MeshActionFactory(name);
                _msm.RegisterAction(name, meshAction);
            }

            // Connect to Discovery Service
            var t1 = _msm.StartDiscoveryServices(_discoveryAddress, cancellationToken);

            return t1;
            // Listen for subscibers for output Qs
            var t2 = _msm.StartListeningServices(cancellationToken);
            //return t2;
            // Connect to publishers of the data we want
            var t3 = _msm.StartPublisherConnections(cancellationToken);

            return Task.WhenAll(t1, t2, t3);
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
