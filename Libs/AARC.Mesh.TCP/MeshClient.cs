using AARC.Mesh.Interface;
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

            Configuration = new ConfigurationBuilder()
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();

            MeshServiceConfig.Server(Configuration, Services);
            SocketServiceConfig.Transport(Services);
        }

        public void BuildServiceProvider() => ServiceProvider = Services.BuildServiceProvider();
        
        public Task Start()
        {
            msm = ServiceProvider.GetService<MeshServiceManager>();

            var cancellationToken = _cts.Token;

            DiscoveryService = msm.StartDiscoveryServices(cancellationToken);
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
