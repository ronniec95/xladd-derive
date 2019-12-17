using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh.Model
{
    public class MeshNetChannel<T> : IChannelObservable<T> where T: class
    {
        private List<IObserver<T>> observers;

        public MeshNetChannel()
        {
            observers = new List<IObserver<T>>();
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

        public IDisposable Subscribe(IObserver<T> observer)
        {
            if (!observers.Contains(observer))
                observers.Add(observer);
            return new Unsubscriber<IObserver<T>>(observers, observer);
        }

        public void Completed()
        {
            foreach (var observer in observers.ToArray())
                if (observers.Contains(observer))
                    observer.OnCompleted();

            observers.Clear();
        }
    }

    public class MeshNetQueueException : Exception
    {
        internal MeshNetQueueException()
        { }
    }
}
