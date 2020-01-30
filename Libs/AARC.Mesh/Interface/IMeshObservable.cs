using System;
using AARC.Mesh.Model;

namespace AARC.Mesh.Interface
{
    public interface IMeshObservable<T> : IObservable<T>, IRouteRegister<MeshMessage> // where T : class, new()
    { }
}