using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections;

namespace AARC.Mesh.Model
{
    using AARC.Mesh.Comparers;
    public class MeshDictionaryObservable<TKey,TValue> : MeshObservable<ConcurrentDictionary<TKey,TValue>>
    {
        private ConcurrentDictionary<TKey, TValue> _source;
        public MeshDictionaryObservable(string channel) : base(channel)
        {
            _source = new ConcurrentDictionary<TKey, TValue>();
        }

        public (IDictionary<TKey, ICollection> Added, IDictionary<TKey, ICollection> Deleted) Changes(ConcurrentDictionary<TKey, TValue> other, Comparer<TValue> comparer = null)
            => _source.Changes<TKey, TValue>(other);
    }
}
