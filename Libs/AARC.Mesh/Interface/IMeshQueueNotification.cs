using System;

namespace AARC.Mesh.Interface
{
    public interface IMeshQueueNotification<T>
    {
        Action<string, T> CollectionChanged { get; set; }
    }
}
