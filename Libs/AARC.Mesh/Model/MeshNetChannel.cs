using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh.Model
{
    public class MeshNetChannel<T> : IChannelObservable<T> where T: class
    {
        private readonly List<IObserver<T>> observers;
        private IChannelProxy onConnectCallbackProxy = null;

        public MeshNetChannel()
        {
            observers = new List<IObserver<T>>();
        }

        public MeshNetChannel(IChannelProxy callBackProxy): this()
        {
            onConnectCallbackProxy = callBackProxy;
        }

        public string Name { get; set; }

        public Action<string, T> CollectionChanged { get; set; }

        public void Add(T item)
        {
            foreach (var observer in observers)
            {
                if (item == null)
                    observer.OnError(new MeshNetQueueException());
                else
                    observer.OnNext(item);
            }
        }

        public void OnConnect(Uri transportUrl) => onConnectCallbackProxy?.OnConnect?.Invoke(transportUrl);

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            return new Unsubscriber(observers, observer);
        }

        public void Completed()
        {
            foreach (var observer in observers.ToArray())
                if (observers.Contains(observer))
                    observer.OnCompleted();

            observers.Clear();
        }

        private class Unsubscriber : IDisposable
        {
            private List<IObserver<T>> _observers;
            private IObserver<T> _observer;

            public Unsubscriber(List<IObserver<T>> observers, IObserver<T> observer)
            {
                this._observers = observers;
                this._observer = observer;
            }

            public void Dispose()
            {
                if (_observer != null && _observers.Contains(_observer))
                    _observers.Remove(_observer);
            }
        }
    }

    public class MeshNetQueueException : Exception
    {
        internal MeshNetQueueException()
        { }
    }
}
