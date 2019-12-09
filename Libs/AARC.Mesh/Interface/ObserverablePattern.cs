using System;
using System.Collections.Generic;

namespace AARC.Mesh.Interface
{

    public interface IPublisher<in T>
    {
        void OnPublish(T value);
    }

    public interface ISubscriber<out T>
    {
        IDisposable Subscribe(IPublisher<T> publisher);
    }

    public class SubscriberPattern<T> : ISubscriber<T>
    {
        public List<IPublisher<T>> _publishers = new List<IPublisher<T>>();

        public IDisposable Subscribe(IPublisher<T> publisher)
        {
            if (!_publishers.Contains(publisher))
                _publishers.Add(publisher);
            return new Unsubscriber<IPublisher<T>>(_publishers, publisher);
        }
    }

    public interface IDuplex<T> : ISubscriber<T>, IPublisher<T> where T: class
    {

    }

    public class ObserverablePattern<T> : IObservable<T>
    {
        public List<IObserver<T>> _observers = new List<IObserver<T>>();

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!_observers.Contains(observer))
                _observers.Add(observer);
            return new Unsubscriber<IObserver<T>> (_observers, observer);
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
