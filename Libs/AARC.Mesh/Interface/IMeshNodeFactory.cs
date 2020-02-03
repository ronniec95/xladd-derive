using AARC.Mesh.Model;

namespace AARC.Mesh.Interface
{
    public interface IMeshNodeFactory
    {
        IMeshReactor<MeshMessage> Get(string service);
    }
}
