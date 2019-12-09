using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using AARC.Mesh.SubService;
using AARC.Mesh.Interface;
using AARC.Mesh.Model;
using System.Collections.Generic;
using AARC.Model.Interfaces;

namespace AARC.Mesh.Client
{
    public class MeshClient : IDisposable
    {
        private readonly ILogger<MeshClient> _logger;
        private readonly MeshServiceManager _msm;
        private readonly Uri _discoveryAddress;
        private MeshObservable<IDictionary<string, IAarcPrice>> nasdaqTP;

        public MeshClient(ILogger<MeshClient> logger, MeshServiceManager meshServiceManager, IConfiguration configuration)
        {

            _msm = meshServiceManager;
            _discoveryAddress = new Uri(configuration.GetValue<string>("ds", "tcp://localhost:9999"));

            _logger = logger;
            // Todo: Bit of a hack as DS should supply port
            _msm.ListeningPort = configuration.GetValue<Int32>("port", 0);

            nasdaqTP = new MeshObservable<IDictionary<string, IAarcPrice>>("nasdaqtestout");
            _msm.RegisterChannels(nasdaqTP);
        }

        /// <summary>
        /// Create Find DiscoveryService
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger?.LogInformation($"Starting {this.GetType().Name}");
            var t1 = _msm.StartDiscoveryServices(_discoveryAddress.ToString(), cancellationToken);
            return t1;

            //return t2;
            // Connect to publishers of the data we want
            //var t3 = _msm.StartPublisherConnections(cancellationToken);

            //return Task.WhenAll(t1, t2, t3);
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
