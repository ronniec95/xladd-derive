using System.Collections.Generic;

namespace AARC.Mesh.Model
{
    using AARC.Mesh.Comparers;
    public class MeshSetObserverable<TSource> : MeshObservable<HashSet<TSource>>
    {
        private HashSet<TSource> source;

        public MeshSetObserverable(string channel) : base(channel)
        {
            source = new HashSet<TSource>();
        }

        public (HashSet<TSource> Added, HashSet<TSource> Deleted) Changes(HashSet<TSource> other) => CompareExtension.SetDifferences(source, other);
    }
}
