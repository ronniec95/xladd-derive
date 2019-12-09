using System;
using System.Collections.Generic;

namespace AARC.Mesh.Interface
{
    /// <summary>
    /// Used with IObserverables or ISubscriber patterns
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Unsubscriber<T> : IDisposable where T : class
    {
        private List<T> _subscribers;
        private T _subscriber;

        public Unsubscriber(List<T> subscribers, T subscriber)
        {
            this._subscribers = subscribers;
            this._subscriber = subscriber;
        }

        public void Dispose()
        {
            if (_subscriber != null && _subscribers.Contains(_subscriber))
                _subscribers.Remove(_subscriber);
        }
    }

    /*public abstract class MeshQueueMarshal : IMessageQueueMarshal<MeshMessage>, IObserver<MeshMessage>
    {
        /// <summary>
        /// Names of Input Queues
        /// </summary>
        public IList<string> InputQueueNames { get; set; }

        /// <summary>
        /// Names of Output Queues
        /// </summary>
        public IList<string> OutputQueueNames { get; set; }

        /// <summary>
        /// Method to post messages to the output queue listeners
        /// </summary>
        public MeshQueueResult<MeshMessage> PostOutputQueue { get; set; }

        /// <summary>
        /// Register input and output queues we want to subscribe to.
        /// Messages
        /// </summary>
        /// <param name="inputQs"></param>
        /// <param name="outputQs"></param>
        public void RegisterDependencies(MeshDictionary<MeshMessage> inputQs, MeshDictionary<MeshMessage> outputQs)
        {
            if (inputQs != null)
                foreach (var route in InputQueueNames)
                    if (!inputQs.ContainsKey(route))
                    {
                        inputQs[route] = new MeshNetQueue<MeshMessage>();
                        Subscribe(inputQs[route]);
                    }

            if (outputQs != null)
                foreach (var route in OutputQueueNames)
                    if (!outputQs.ContainsKey(route))
                        outputQs[route] = new MeshNetQueue<MeshMessage>();
        }

        // IObserver patterm

        protected IDisposable unsubscriber;
        public virtual void Subscribe(IObservable<MeshMessage> provider)
        {
            if (provider != null)
                unsubscriber = provider.Subscribe(this);
        }

        public void OnCompleted() => Unsubscribe();

        public abstract void OnError(Exception error);

        /// <summary>
        /// Method used to decode the Message Payload and called the business logic
        /// </summary>
        /// <param name="item"></param>
        public abstract void OnNext(MeshMessage item);

        /// <summary>
        /// When a client connects for the first time allow the services to send over
        /// initial data
        /// </summary>
        public abstract void OnConnect();

        public virtual void Unsubscribe() => unsubscriber?.Dispose();
    }*/
}
