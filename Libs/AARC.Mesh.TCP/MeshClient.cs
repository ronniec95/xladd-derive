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
        static CancellationTokenSource _cts = new CancellationTokenSource();
        public IServiceProvider ServiceProvider;
        public Task DiscoveryService;
        public Task ChannelSubscriber;
        public Task ChannelPublisher;
        public MeshClient(string[] args)
        {
            var cancellationToken = _cts.Token;

            var services = new ServiceCollection()
                .AddLogging()
                .AddOptions();

            MeshServiceConfig.Server(services);
            SocketServiceConfig.Transport(services);

            ServiceProvider = services.BuildServiceProvider();

            var config = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            ServiceProvider.GetService<ILoggerFactory>()
                .AddLog4Net();

            msm = ServiceProvider.GetService<MeshServiceManager>();

            var discoveryAddress = new Uri(config.GetValue<string>("ds", "tcp://localhost:9999"));

            // Todo: Bit of a hack as DS should supply port
            msm.ListeningPort = config.GetValue<Int32>("port", 0);

            DiscoveryService = msm.StartDiscoveryServices(discoveryAddress, cancellationToken);
            // Listen for subscibers for output Qs
            ChannelSubscriber = msm.StartListeningServices(cancellationToken);
            // Connect to publishers of the data we want
            ChannelPublisher = msm.StartPublisherConnections(cancellationToken);
        }
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
