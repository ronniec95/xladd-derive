using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace AARC.Mesh.SubService
{
    using AARC.Mesh.Interface;
    using AARC.Mesh.Model;

    /// <summary>
    /// An adaptor proxy between the business processinmg code and the transport
    /// through input and output qs
    /// </summary>
    public class MeshServiceManager : IDisposable
    {
        protected readonly DiscoveryServiceStateMachine<MeshMessage> _dssmc;
        protected readonly ILogger _logger;

        /// <summary>
        /// Action method wrappers
        /// </summary>
        public ConcurrentDictionary<string, MeshQueueMarshal> _actions = new ConcurrentDictionary<string, MeshQueueMarshal>();

        public delegate void MeshMessagePublisherDelegate(string transportId, MeshMessage message);

        public MeshMessagePublisherDelegate ServicePublisher { get; set; }

        public int ListeningPort { get { return _dssmc.Port; } set { _dssmc.Port = value; } }
        private IMeshTransport<MeshMessage> _meshTransport;
        private DiscoveryMonitor<DiscoveryMessage> _discoveryMonitor;

        public MeshServiceManager(ILogger<MeshServiceManager> logger, DiscoveryServiceStateMachine<MeshMessage> discoveryServiceState, DiscoveryMonitor<DiscoveryMessage> discoveryManager, IMeshTransport<MeshMessage> meshTransport)
        {
            _dssmc = discoveryServiceState;
            _logger = logger;
            _discoveryMonitor = discoveryManager;
            _discoveryMonitor.DiscoveryReceive = _dssmc.Receive;
            _discoveryMonitor.DiscoverySend = _dssmc.Send;

            _meshTransport = meshTransport;
            _meshTransport.Subscribe += ServiceSubscriber;
            ServicePublisher += _meshTransport.Publisher;
        }

        /// <summary>
        /// Register action and q required
        /// </summary>
        /// <param name="name"></param>
        /// <param name="meshAction"></param>
        public void RegisterAction(string name, MeshQueueMarshal meshAction)
        {
            _actions[name] = meshAction;
            meshAction.RegisterDependencies(_dssmc.inputQs, _dssmc.outputQs);
            meshAction.PostOutputQueue += PostResult;
        }

        /// <summary>
        /// Now we have a return path for our action processor
        /// </summary>
        /// <param name="queuename">output queue</param>
        /// <param name="message">return message/result</param>
        private void PostResult(string queuename, MeshMessage message)
        {
            // Check this is one of our actions
            if (_dssmc.outputQs.ContainsKey(queuename))
            {
                // Is it for external consumption?
                if (_dssmc.OutputQsRoutes.ContainsKey(queuename))
                {
                    _dssmc.outputQs[queuename].Add(message);
                    message.Service = _meshTransport.TransportId;
                    message.QueueName = queuename;

                    var routes = _dssmc.InputQueueRoute(queuename);
                    if (routes == null || !routes.Any())
                        _logger.LogWarning($"NO ROUTE Message GraphId={message.GraphId}, Xid={message.XId}, Queue={queuename}");
                    else
                    foreach (var route in routes)
                    {
                        // Todo: Unique Xid for each route? or the same to group?
                        // Find connection if we have it
                        ServicePublisher?.Invoke(route, message);
                    }
                }
                else
                {
                    // Todo: No destinations available? Keep on the Q? How to process later?
                    _logger.LogInformation($"NO ROUTE for {queuename}");
                }
            }
            else
                // Todo: Unknown destination?
                _logger.LogInformation($"UNKOWN ROUTE for {queuename}");
        }

        /// <summary>
        /// When a new MeshMessage arrives check for a match of the action against the input q.
        /// </summary>
        /// <param name="message"></param>
        public void ServiceSubscriber(MeshMessage message)
        {
            // Todo: Has the message come from a known external source? If not what to do?
            var known = _dssmc.RegisteredInputSource(message.QueueName, message.Service);

            if (_dssmc.inputQs.ContainsKey(message.QueueName))
            {
                _dssmc.inputQs[message.QueueName].Add(message);
            }
        }

        public async Task StartDiscoveryServices(ServiceHost serviceHost, CancellationToken cancellationToken) => await _discoveryMonitor.StartListeningServices(serviceHost, cancellationToken);

        /// <summary>
        /// down stream services will lookup the output queues from the DS and find out IP address and connect.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartListeningServices(CancellationToken cancellationToken) => await _meshTransport.StartListeningServices(_dssmc.Port, cancellationToken);

        public async Task StartPublisherConnections(CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var routableInputQs = _dssmc.RoutableInputQs();
                    foreach (var routes in _dssmc.InputQRoutes)
                    {
                        _logger?.LogInformation($"ROUTE(i) [{string.Join(",", routes.Item2)}]=>{routes.Item1}=>[{_meshTransport.TransportId}]");
                    }

                    foreach (var routes in _dssmc.OutputQRoutes)
                    {
                        _logger?.LogInformation($"ROUTE(o) [{_meshTransport.TransportId}]=>{routes.Item1}=>[{string.Join(",", routes.Item2)}]");
                    }

                    var routableAddresses = _dssmc.OutputQRoutes.SelectMany(r => r.Item2).Distinct();
                    foreach (var address in routableAddresses)
                        try
                        {
                            if (_meshTransport.TransportId != address)
                            {
                                // Connect to service and register our input qs
                                _meshTransport.ServiceConnect(address, cancellationToken);
                            }
                        }
                        catch (Exception e)
                        {
                            _logger?.LogError(e, "MSM General Error with connections");
                        }
                        finally
                        {
                            Task.Delay(1000).Wait();
                        }
                    Task.Delay(30000).Wait();
                }
            }, cancellationToken);
        }

        public async Task Cancel() => await _meshTransport.Cancel();

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _discoveryMonitor.Dispose();
                    _discoveryMonitor = null;

                    _meshTransport.Dispose();
                    _meshTransport = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~MeshServiceManager()
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
