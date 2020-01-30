using System;

namespace AARC.Mesh.Interface
{
    public interface ISubscriber<out T>
    {
        IDisposable Subscribe(IPublisher<T> publisher);
    }
}
