using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AARC.Mesh.SubService
{
    using System.Collections.Generic;
    using AARC.Mesh.Interface;
    using AARC.Mesh.Model;

    /// <summary>
    /// An adaptor proxy between the business processinmg code and the transport
    /// through input and output qs
    /// </summary>
    public class MeshServiceManager : IObserver<MeshMessage>
    {
        protected readonly DiscoveryServiceStateMachine<MeshMessage> _dssmc;
        protected readonly ILogger _logger;

        private IMeshTransport<MeshMessage> _transportServer;
        private readonly IMonitor _monitor;
        private DiscoveryMonitor<DiscoveryMessage> _discoveryMonitor;

        public Task DiscoveryService { get; private set; }
        public Task ListenService { get; private set; }
        public Task PublishService { get; private set; }

        public MeshServiceManager(ILogger<MeshServiceManager> logger, DiscoveryServiceStateMachine<MeshMessage> discoveryServiceState, DiscoveryMonitor<DiscoveryMessage> discoveryMonitor, IMonitor monitor, IMeshTransport<MeshMessage> meshTransport)
        {
            _dssmc = discoveryServiceState;
            _logger = logger;
            _discoveryMonitor = discoveryMonitor;
            _discoveryMonitor.ResetDiscoveryState += _dssmc.ResetState;
            _discoveryMonitor.DiscoveryReceiveMessage += _dssmc.CreateReceiveMessage;
            _discoveryMonitor.DiscoverySendMessage += _dssmc.CreateSendMessage;
            _discoveryMonitor.DiscoveryErrorMessage += _dssmc.CreateErrorMessage;

            _transportServer = meshTransport;

            _monitor = monitor;

            // MeshMessages from transportserver
            // Todo: But this should really be any transportservice {socketservice}
            _transportServer.Subscribe(this);
        }

        protected ConcurrentDictionary<string, IList<IRouteRegister<MeshMessage>>> routeLookup = new ConcurrentDictionary<string, IList<IRouteRegister<MeshMessage>>>();
        protected ConcurrentDictionary<string, IList<string>> serviceLookup = new ConcurrentDictionary<string, IList<string>>();


        /// <summary>
        /// Register action and q required
        /// </summary>
        /// <param name="route"></param>
        public void RegisterChannels(IRouteRegister<MeshMessage> route)
        {
            route.RegisterReceiverChannels(_dssmc.LocalInputChannels);
            route.RegistePublisherChannels(_dssmc.LocalOutputChannels);
            route.RegisterMonitor(_monitor);

            route.PublishChannel += OnNext;
        }

        public ManualResetEvent RegistrationComplition { get { return _dssmc.RegistrationComplete; } }

        /// <summary>
        /// We are looking to see if there is an input route for our output for'channel'
        /// </summary>
        /// <param name="channel">output channel</param>
        /// <param name="message">return message/result</param>
        private void OnNext(string channel, MeshMessage message)
        {
            // Check this is one of our channels
            var channelRoute = _dssmc.OutputChannelMap.Where(r => r.Item1 == channel);
            if (channelRoute.Any())
            {
                // Todo: This causes feedback what do I want to do here?
                //_dssmc.outputChannels[channel].Add(message);
                message.Service = _transportServer.URI;
                message.Channel = channel;
                message.State = MeshMessage.States.MessageOut;

                // Find the external routes
                var routes = channelRoute.SelectMany(c => c.Item2);
                if (routes == null || !routes.Any())
                {
                    var status = $"NO ROUTE Message GraphId={message.GraphId}, Xid={message.XId}, Channel={channel}";
                    _monitor.OnInfo(status, channel);
                    _logger.LogWarning(status);
                }
                else
                {
                    if (message.Routes == null)
                        message.Routes = routes;
                    else
                    {
                        var intersection = message.Routes.Intersect(routes);
                        if (intersection.Any())
                            _logger.LogInformation($"ROUTE CONFIRMED Message GraphId={message.GraphId}, Xid={message.XId}, Channel={channel} Routes={string.Join(",", intersection)}");
                        else
                        // Todo: Tell Monitor?
                        {
                            var status = $"ROUTE NOT FOUND Message GraphId={message.GraphId}, Xid={message.XId}, Channel={channel} Routes={string.Join(",", message.Routes)}";
                            _monitor.OnInfo(status, channel);
                            _logger.LogWarning(status);
                        }
                    }
                    _transportServer.OnNext(message);
                }
            }
            else
            {
                var status = $"[{channel}] NO ROUTE";
                _monitor.OnInfo(status, channel);
                _logger.LogInformation(status);
            }
        }

        /// <summary>
        /// When a new MeshMessage arrives check for a match of the action against the input q.
        /// </summary>
        /// <param name="message"></param>
        public void ServiceSubscriber(MeshMessage message)
        {
            // Todo: Has the message come from a known external source? If not what to do?
            var known = _dssmc.RegisteredInputSource(message.Channel, message.Service);

            if (_dssmc.LocalInputChannels.ContainsKey(message.Channel))
            {
                _dssmc.LocalInputChannels[message.Channel].Add(message);
            }
        }

        public Task StartService(CancellationToken token)
        {
            DiscoveryService = StartDiscoveryServices(token);
            ListenService = StartListeningServices(token);
            PublishService = StartPublisherConnections(token);
            return Task.WhenAll(DiscoveryService, ListenService, PublishService);
        }

        public async Task StartDiscoveryServices(CancellationToken cancellationToken) => await _discoveryMonitor.StartListeningServices(cancellationToken);

        /// <summary>
        /// down stream services will lookup the output queues from the DS and find out IP address and connect.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task StartListeningServices(CancellationToken cancellationToken)
        {
            RegistrationComplition.WaitOne();
            _logger.LogInformation($"Listener Enabled on {_dssmc.Port}");
            _transportServer.SetPort(_dssmc.Port);
            _monitor.URI = _transportServer.URI;
            await _transportServer.StartListeningServices(cancellationToken);
        }

        public async Task StartPublisherConnections(CancellationToken cancellationToken)
        {
            await Task.Factory.StartNew(() =>
            {
                RegistrationComplition.WaitOne();
                try
                {
                    _logger.LogInformation("Publisher Enabled");
                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var routableInputQs = _dssmc.RoutableInputChannels();
                        foreach (var routes in _dssmc.IputChannelMap)
                        {
                            _logger?.LogInformation($"ROUTE(i) [{string.Join(",", routes.Item2)}]=>{routes.Item1}=>[{_transportServer.URI}]");
                        }

                        foreach (var routes in _dssmc.OutputChannelMap)
                        {
                            _logger?.LogInformation($"ROUTE(o) [{_transportServer.URI}]=>{routes.Item1}=>[{string.Join(",", routes.Item2)}]");
                        }

                        var channelTransports = _dssmc.OutputChannelMap.SelectMany(r => r.Item2).Distinct();
                        foreach (var transportUrl in channelTransports)
                            try
                            {
                                if (_transportServer.URI != transportUrl)
                                {
                                    // If new connection and connected
                                    if (_transportServer.ServiceConnect(transportUrl, cancellationToken))
                                    {
                                        // Need Channel Names
                                        //_dssmc.OutputChannelRoutes[]
                                        var channels = _dssmc.OutputChannelMap.Where(r => r.Item2.Contains(transportUrl)).Select(r => r.Item1);
                                        _logger?.LogInformation($"{transportUrl} OnConnect {string.Join(",", channels)}");
                                        foreach (var c in channels)
                                        {
                                            if (_dssmc.LocalOutputChannels.ContainsKey(c))
                                            {
                                                var o = _dssmc.LocalOutputChannels[c];
                                                o.OnConnect(transportUrl);
                                            }
                                        }
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                OnError(e);
                                _logger?.LogError(e, "MSM General Error with connections");
                            }
                            finally
                            {
                                Task.Delay(1000).Wait();
                            }
                        Task.Delay(5000).Wait();
                    }
                }
                catch(Exception ex)
                {
                    OnError(ex);
                    _logger?.LogError(ex, "MSM Route determin errors");
                }
            }, cancellationToken);
        }

        public async Task Cancel() => await _transportServer.Cancel();

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Task.WaitAll(DiscoveryService, ListenService, PublishService);

                    _discoveryMonitor.Dispose();
                    _discoveryMonitor = null;

                    _transportServer.Dispose();
                    _transportServer = null;
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

        // ToDo: To Monitor?
        public void OnCompleted()
        {
            throw new NotImplementedException();
        }
        
        public void OnError(Exception error) => _monitor?.OnError(error, "ERROR");

        /// <summary>
        /// Messages from transportserver need to be sent to MeshObservables
        /// </summary>
        /// <param name="value"></param>
        public void OnNext(MeshMessage value)
        {
            _dssmc.LocalInputChannels[value.Channel].Add(value);
        }
        #endregion
    }
}
