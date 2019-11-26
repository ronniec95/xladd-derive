using System;
using System.Collections.Generic;
using AARC.Mesh.Model;

namespace AARC.Mesh.Interface
{
    public abstract class MeshQueueMarshal : IMessageQueueMarshal<MeshMessage>, IObserver<MeshMessage>
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
            foreach (var route in InputQueueNames)
                if (!inputQs.ContainsKey(route))
                {
                    inputQs[route] = new MeshNetQueue<MeshMessage>();
                    Subscribe(inputQs[route]);
                }

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

        public virtual void Unsubscribe() => unsubscriber?.Dispose();
    }
}
