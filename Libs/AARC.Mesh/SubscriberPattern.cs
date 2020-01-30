using System;
using System.Collections.Generic;
using AARC.Mesh.Interface;

namespace AARC.Mesh
{
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
}
