namespace AARC.Mesh.Interface
{
    public interface IMeshService
    {
        string EndPoint { get; }
        byte[] GetData();
        void PutData(byte[] data);
    }
}