using System;
using System.Collections.Generic;

namespace AARC.Mesh
{
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
}
