using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;
using Newtonsoft.Json;

namespace AARC.Mesh.Model
{
    public class MeshChannelProxy<T> : ObserverablePattern<T>, IRouteRegister<MeshMessage>, IObserver<MeshMessage>, IChannelProxy
    {
        private readonly ChannelMetrics _metrics;

        /// <summary>
        /// Input Channel Names are past to the discovery service to help client/servers find matches
        /// </summary>
        public string InputChannelAlias { get; }

        /// <summary>
        ///Output Channel Names are past to the discovery service to help client/servers find matches
        /// </summary>
        public string OutputChannelAlias { get; }

        /// <summary>
        /// Used to notifed the a new connection has been made.
        /// </summary>
        public Action<string> OnConnect { get; set; }

        public int ClusterType { get; }

        private MeshChannelProxy()
        {
            _metrics = new ChannelMetrics { ReturnType = typeof(T) };
            _observers = new List<IObserver<T>>();
        }

        public MeshChannelProxy(string inputChannelName = null, string outputChannelName = null, int clusterType = 0)
            : this()
        {
            var channelType = typeof(T);

            ClusterType = clusterType;

            if (!string.IsNullOrEmpty(inputChannelName))
            {
                _metrics.Name = $"{inputChannelName}({channelType})";
                this.InputChannelAlias = _metrics.Name;
            }
            if (!string.IsNullOrEmpty(outputChannelName))
            {
                _metrics.Name = $"{outputChannelName}({channelType})";
                this.OutputChannelAlias = _metrics.Name;
            }
        }

        /// <summary>
        /// Method to post messages to the output channel listeners
        /// </summary>
        public MeshChannelResult<MeshMessage> PublishChannel { get; set; }

        /// <summary>
        /// Register input and output queues we want to subscribe to.
        /// Messages
        /// </summary>
        /// <param name="inputChannels"></param>
        /// <param name="outputChannels"></param>
        public void RegisterDependencies(MeshDictionary<MeshMessage> inputChannels = null, MeshDictionary<MeshMessage> outputChannels = null)
        {
            RegisterReceiverChannels(inputChannels);

            RegistePublisherChannels(outputChannels);
        }

        /// <summary>
        /// Register receiver channels and subscribe to updates
        /// </summary>
        /// <param name="inputChannels"></param>
        public void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputChannels)
        {
            if (inputChannels != null)
                if (!inputChannels.ContainsKey(InputChannelAlias))
                {
                    inputChannels[InputChannelAlias] = new MeshNetChannel<MeshMessage>();
                    Subscribe(inputChannels[InputChannelAlias]);
                }
        }

        public void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels)
        {
            if (outputChannels != null)
                if (!outputChannels.ContainsKey(OutputChannelAlias))
                {
                    outputChannels[OutputChannelAlias] = new MeshNetChannel<MeshMessage>(this);
                }
        }

        /// <summary>
        /// Transport errors should be sent to the DS
        /// </summary>
        /// <param name="error"></param>
        public void OnError(Exception error)
        {
            ++_metrics.Errors;
            // Todo: Send to DS
            //throw new NotImplementedException();
        }

        public void OnNext(MeshMessage item)
        {
            // Message convertion we send throw to the user
            T payload = default;
            payload = JsonConvert.DeserializeObject<T>(item.PayLoad);

            // Transport errors we throw to DS
            ++_metrics.NoMsgReceived;
            try
            {
                foreach (var observer in _observers)
                    observer.OnNext(payload);
            }
            catch (Exception ex)
            {
                OnError(ex);
            }
        }

        private IDisposable unsubscriber;
        public void Subscribe(IObservable<MeshMessage> provider)
        {
            if (provider != null)
                unsubscriber = provider.Subscribe(this);
        }
        public void OnCompleted() => Unsubscribe();

        public void Unsubscribe() => unsubscriber?.Dispose();
        // To Transport out
        public void OnPost(T payload, string transportUrl = null)
        {
            ++_metrics.NoMsgSent;
            var jpayload = JsonConvert.SerializeObject(payload);
            var xid = MeshUtilities.NewXId;
            var message = new MeshMessage { GraphId = 1, XId = xid, PayLoad = jpayload };
            if (!string.IsNullOrEmpty(transportUrl))
                message.Routes = new List<string> { transportUrl };
            PublishChannel?.Invoke(this.OutputChannelAlias, message);
        }
    }
}
