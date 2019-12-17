namespace AARC.Mesh.Interface
{
    public interface IDuplex<T> : ISubscriber<T>, IPublisher<T> where T : class
    {

    }
}
