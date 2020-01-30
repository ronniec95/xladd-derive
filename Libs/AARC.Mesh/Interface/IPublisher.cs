namespace AARC.Mesh.Interface
{
    public interface IPublisher<in T>
    {
        void OnPublish(T value);
    }
}
