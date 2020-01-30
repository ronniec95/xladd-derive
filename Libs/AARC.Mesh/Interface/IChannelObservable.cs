namespace AARC.Mesh.Interface
{
    public interface IChannelObservable<T> : System.IObservable<T>
    {
        string Name { get; set; }
        void Add(T item);
        void OnConnect(string transportUrl);
    }
}
