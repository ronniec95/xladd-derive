using System.Collections.Generic;


namespace AARC.Mesh
{
    public interface IByteEnumerator
    {
        IEnumerable<byte[]> GetEnumerable();
    }
}