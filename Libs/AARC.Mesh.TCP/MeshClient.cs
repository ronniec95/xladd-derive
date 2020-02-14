using AARC.Mesh.Model;
using AARC.Mesh.SubService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace AARC.Mesh.TCP
{
    public class MeshClient : IDisposable
    {
        MeshServiceManager msm;
        private readonly Uri discoveryAddress;
        static CancellationTokenSource _cts = new CancellationTokenSource();
        public IServiceProvider ServiceProvider;
        public readonly IConfigurationRoot Configuration;
        public IServiceCollection Services;
        public Task DiscoveryService;
        public Task ChannelSubscriber;
        public Task ChannelPublisher;
        public MeshClient(string[] args)
        {
            Services = new ServiceCollection()
                .AddLogging()
                .AddOptions();

            MeshServiceConfig.Server(Services);
            SocketServiceConfig.Transport(Services);

            Configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            discoveryAddress = new Uri(Configuration.GetValue<string>("ds", "tcp://localhost:9999"));
        }

        public void BuildServiceProvider() => ServiceProvider = Services.BuildServiceProvider();
        
        public Task Start()
        {
            var logger = GetLogger<MeshClient>();
            msm = ServiceProvider.GetService<MeshServiceManager>();
            // Todo: Bit of a hack as DS should supply port
            msm.ListeningPort = Configuration.GetValue<Int32>("port", 0);

            logger.LogInformation($"Discovery Service {discoveryAddress}");
            logger.LogInformation($"Listening Address {msm.ListeningPort}");

            var cancellationToken = _cts.Token;

            DiscoveryService = msm.StartDiscoveryServices(discoveryAddress, cancellationToken);
            // Listen for subscibers for output Qs
            ChannelSubscriber = msm.StartListeningServices(cancellationToken);
            // Connect to publishers of the data we want
            ChannelPublisher = msm.StartPublisherConnections(cancellationToken);

            return Task.WhenAll(DiscoveryService, ChannelSubscriber, ChannelPublisher);
        }

        public ILogger<T> GetLogger<T>() => ServiceProvider.GetService<ILoggerFactory>().CreateLogger<T>();

        public IObservable<T> CreateObservable<T>(string channel) where T : class, new()
        {
            var observable = new MeshObservable<T>(channel);
            msm.RegisterChannels(observable);
            return observable;
        }

        public IObserver<T> CreateObserver<T>(string channel)// where T : class, new()
        {
            var observer = new MeshObserver<T>(channel);
            msm.RegisterChannels(observer);
            return observer;
        }

        public void Stop()
        {
            if (!_cts.IsCancellationRequested)
                _cts.Cancel();
            Task.WaitAll(DiscoveryService, ChannelPublisher, ChannelSubscriber);
            DiscoveryService = Task.CompletedTask;
            ChannelPublisher = Task.CompletedTask;
            ChannelSubscriber = Task.CompletedTask;
        }
        public void Dispose() => Stop();
    }
}
