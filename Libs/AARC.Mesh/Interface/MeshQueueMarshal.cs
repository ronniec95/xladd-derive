using System;
using System.Collections.Generic;
using AARC.Mesh.Model;
using Newtonsoft.Json;

namespace AARC.Mesh.Interface
{
    public class MeshService<T> : ObserverablePattern<MeshMessage>, IObserver<MeshMessage> where T: class
    {
        private IObserver<T> _parent;

        private IDisposable _unsubscriber;

        public MeshService(IObserver<T> o)
        {
            _parent = o;
        }

        public void Subscriber(IObservable<MeshMessage> provider)
        {
            if (provider != null)
                _unsubscriber = provider.Subscribe(this);
        }

        public void OnCompleted()
        {
            throw new NotImplementedException();
        }

        public void OnError(Exception error)
        {
            throw new NotImplementedException();
        }

        public void OnNext(MeshMessage value)
        {
            var payload = JsonConvert.DeserializeObject<T>(value.PayLoad);
            _parent.OnNext(payload);
        }
    }


    public class MeshChannelProxy<T> : ObserverablePattern<T>, IRouteRegister<MeshMessage>, IObserver<MeshMessage>
    {
        /// <summary>
        /// Names of Input Queues
        /// </summary>
        public IList<string> InputChannelNames { get; set; }

        /// <summary>
        /// Names of Output Queues
        /// </summary>
        public IList<string> OutputChannelNames { get; set; }

        public MeshChannelProxy(string inputq = null, string outputq = null)
        {
            _observers = new List<IObserver<T>>();
            this.InputChannelNames = new List<string>();
            if (!string.IsNullOrEmpty(inputq))
                this.InputChannelNames.Add(inputq);
            this.OutputChannelNames = new List<string>();
            if (!string.IsNullOrEmpty(outputq))
                this.OutputChannelNames.Add(outputq);
        }

        /// <summary>
        /// Method to post messages to the output queue listeners
        /// </summary>
        public MeshChannelResult<MeshMessage> PublishChannel { get; set; }

        /// <summary>
        /// Register input and output queues we want to subscribe to.
        /// Messages
        /// </summary>
        /// <param name="inputQs"></param>
        /// <param name="outputQs"></param>
        public void RegisterDependencies(MeshDictionary<MeshMessage> inputQs = null, MeshDictionary<MeshMessage> outputQs = null)
        {
            RegisterReceiverChannels(inputQs);

            RegistePublisherChannels(outputQs);
        }

        /// <summary>
        /// Register receiver channels and subscribe to updates
        /// </summary>
        /// <param name="inputQs"></param>
        public void RegisterReceiverChannels(MeshDictionary<MeshMessage> inputQs)
        {
            if (inputQs != null)
                foreach (var route in InputChannelNames)
                    if (!inputQs.ContainsKey(route))
                    {
                        inputQs[route] = new MeshNetChannel<MeshMessage>();
                        Subscribe(inputQs[route]);
                    }
        }

        public void RegistePublisherChannels(MeshDictionary<MeshMessage> outputQs)
        {
            if (outputQs != null)
                foreach (var route in OutputChannelNames)
                    if (!outputQs.ContainsKey(route))
                        outputQs[route] = new MeshNetChannel<MeshMessage>();
        }

        public void OnConnect()
        {
            /*
            var payload = item?.PayLoad as T;
            foreach (var observer in observers)
                observer.OnNext(payload);
                */
        }

        public void OnError(Exception error)
        {
            //throw new NotImplementedException();
        }

        public void OnNext(MeshMessage item)
        {
            try
            {
                var payload = JsonConvert.DeserializeObject<T>(item.PayLoad);

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
