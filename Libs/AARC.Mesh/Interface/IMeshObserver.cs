using System;
using AARC.Mesh.Model;

namespace AARC.Mesh.Interface
{
    public interface IMeshObserver<T> : IObserver<T>, IRouteRegister<MeshMessage> { }
}