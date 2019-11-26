namespace AARC.Mesh.Interface
{
    public interface INetQueueObservable<T> : System.IObservable<T>
    {
        string Name { get; set; }
        void Add(T item);
    }
}
