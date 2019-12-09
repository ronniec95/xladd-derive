using System;
using System.Collections.Generic;

namespace AARC.Mesh.Interface
{
    public interface IMeshReactor<T> where T: class
    {
        string Name { get; }
        IList<IRouteRegister<T>> Queues { get; }
    }
}
