//using System;

namespace AARC.Model.Interfaces
{
    //public interface IFastSerialiser
    //{
    //    Type GenericTypeArgument { get; }
    //}

    public interface IFastSerialiser<T>// : IFastSerialiser
    {
        T Load(string path);
        void Save(T item, string path);
    }
}
