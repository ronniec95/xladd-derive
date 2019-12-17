using System;
using System.Collections.Generic;
using AARC.Mesh.Model;
using Newtonsoft.Json;

namespace AARC.Mesh.Interface
{
    public class MeshChannelProxy<T> : ObserverablePattern<T>, IRouteRegister<MeshMessage>, IObserver<MeshMessage>
    {
        /// <summary>
        /// Input Channel Names are past to the discovery service to help client/servers find matches
        /// </summary>
        public IList<string> InputChannelNames { get; set; }

		/// <summary>
		///Output Channel Names are past to the discovery service to help client/servers find matches
		/// </summary>
		public IList<string> OutputChannelNames { get; set; }

        public MeshChannelProxy(string inputChannelName = null, string outputChannelName = null)
        {
            _observers = new List<IObserver<T>>();
            this.InputChannelNames = new List<string>();
            if (!string.IsNullOrEmpty(inputChannelName))
                this.InputChannelNames.Add(inputChannelName);
            this.OutputChannelNames = new List<string>();
            if (!string.IsNullOrEmpty(outputChannelName))
                this.OutputChannelNames.Add(outputChannelName);
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
                foreach (var route in InputChannelNames)
                    if (!inputChannels.ContainsKey(route))
                    {
                        inputChannels[route] = new MeshNetChannel<MeshMessage>();
                        Subscribe(inputChannels[route]);
                    }
        }

        public void RegistePublisherChannels(MeshDictionary<MeshMessage> outputChannels)
        {
            if (outputChannels != null)
                foreach (var route in OutputChannelNames)
                    if (!outputChannels.ContainsKey(route))
                        outputChannels[route] = new MeshNetChannel<MeshMessage>();
        }

        public void OnConnect()
        {
        }

        public void OnError(Exception error)
        {
            //throw new NotImplementedException();
        }

        public void OnNext(MeshMessage item)
        {
            // If the payload fails to serialize then throw it to the user
            // Todo: If the transport fails....
            var payload = JsonConvert.DeserializeObject<T>(item.PayLoad);
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

        public void OnPost(T payload)
        {
            var jpayload = JsonConvert.SerializeObject(payload);
            var xid = MeshUtilities.NewXId;
            var message = new MeshMessage { GraphId = 1, XId = xid, PayLoad = jpayload };
            foreach (var channel in this.OutputChannelNames)
                PublishChannel?.Invoke(channel, message);
        }
    }
}
