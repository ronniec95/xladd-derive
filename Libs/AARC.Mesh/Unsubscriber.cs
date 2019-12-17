using System;
using System.Collections.Generic;

namespace AARC.Mesh
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
}
