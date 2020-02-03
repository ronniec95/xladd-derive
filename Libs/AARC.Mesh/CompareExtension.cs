using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AARC.Mesh.Comparers
{
    public static class CompareExtension
    {
        public static int TValueCompare<TValue>(this TValue source, TValue other, Comparer<TValue> comparer = null)
        {
           if (comparer != null)
                return comparer.Compare(source, other);

            if (IsCollectionType(typeof(TValue)))
                return CompareCollection(source, other);

            Comparer<TValue> defaultComparer = Comparer<TValue>.Default;

            return defaultComparer.Compare(source, other);
        }

        public static int CompareCollection<TValue>(TValue source, TValue other)
        {
            var itemType = CollectionItemType(typeof(TValue));
            //            var constructed = typeof(HashSet<>).MakeGenericType(new Type[] { itemType });
            //            var s = Activator.CreateInstance(constructed, source as ICollection);

            var s = source as ICollection;
            var o = other as ICollection;

            if (s?.Count != o?.Count)
                return -1;

            return 0;
        }

        public static (HashSet<TSource> Added, HashSet<TSource> Deleted) SetDifferences<TSource>(HashSet<TSource> source, HashSet<TSource> other)
        {
            var setOfValuesUpdate = other;
            var intersected = source.Intersect(setOfValuesUpdate);
#if NETSTANDARD2_1
            var changeDeleted = source.Except(setOfValuesUpdate).ToHashSet();
            var changeAdded = setOfValuesUpdate.Except(source).ToHashSet();
#else
            var changeDeleted = new HashSet<TSource>(source.Except(setOfValuesUpdate));
            var changeAdded = new HashSet<TSource>(setOfValuesUpdate.Except(source));
#endif
            source.IntersectWith(intersected);
            source.UnionWith(changeAdded);

            return (changeAdded, changeDeleted);
        }

        public static (IDictionary<TKey, ICollection> Added, IDictionary<TKey, ICollection> Deleted) Changes<TKey, TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> other)
        {
            var comparitor = EqualityComparer<TValue>.Default;
            return source.Changes<TKey, TValue>(other, comparitor);
        }


        public static (IDictionary<TKey, ICollection> Added, IDictionary<TKey, ICollection> Deleted) Changes<TKey,TValue>(this IDictionary<TKey, TValue> source, IDictionary<TKey, TValue> other, EqualityComparer<TValue> comparitor)
        {
#if NETSTANDARD2_1
            var intersected = source.Keys.Where(other.ContainsKey).ToHashSet();

            var keysDeleted = source.Keys.Except(other.Keys).ToHashSet();
            var keysAdded = other.Keys.Except(source.Keys).ToHashSet();
#else
            var intersected = new HashSet<TKey>(source.Keys.Where(other.ContainsKey));
            var keysDeleted = new HashSet<TKey>(source.Keys.Where(d => !other.ContainsKey(d)));
            var keysAdded = new HashSet<TKey>(other.Keys.Where(u => !source.ContainsKey(u)));
#endif
            var added = new Dictionary<TKey, ICollection>();
            var deleted = new Dictionary<TKey, ICollection>();
            var isEnumerable = IsEnumerableType(typeof(TValue));
            foreach (var key in intersected)
            {
                var sourceValues = source[key];
                var otherValues = other[key];
                if (isEnumerable)
                {
                    var (diffSource, diffOther) = CompareExtension.Changes(source[key] as IEnumerable, other[key] as IEnumerable);
                    if (diffSource?.Count > 0)
                        added[key] = diffSource;

                    if (diffOther?.Count > 0)
                        deleted[key] = diffOther;
                }
                // else entity
            }
            var itemType = CompareExtension.CollectionItemType(typeof(TValue));
            foreach (var key in keysDeleted)
                deleted[key] = CompareExtension.ToCollection(source[key], itemType);

            foreach (var key in keysAdded)
                added[key] = CompareExtension.ToCollection(other[key], itemType);

            return (added, deleted);
        }

        public static (ICollection Added, ICollection Deleted) Changes(IEnumerable source, IEnumerable other)
        {
            var itemType = CollectionItemType(source.GetType());
            var settype = typeof(HashSet<>).MakeGenericType(new Type[] { itemType });
            var sourceSet = Activator.CreateInstance(settype, source);
            var otherSet = Activator.CreateInstance(settype, other);

            var extensionsType = typeof(CompareExtension);
            var method = extensionsType.GetMethod("SetDifferences");
            var generic = method.MakeGenericMethod(itemType);
            object o = generic.Invoke(sourceSet, new object[] { sourceSet, otherSet });
            var added = o.GetType().GetField("Item1").GetValue(o);
            var deleted = o.GetType().GetField("Item2").GetValue(o);

            return (ToCollection(added, itemType), ToCollection(deleted, itemType));
        }

        public static ICollection ToCollection(object source, Type itemType)
        {
            var collectionType = typeof(List<>).MakeGenericType(new Type[] { itemType });
            var collection = Activator.CreateInstance(collectionType, source);
            return collection as ICollection;
        }

        public static bool IsEnumerableType(Type type) => (type.GetInterface(nameof(IEnumerable)) != null);
        public static bool IsCollectionType(Type type) => (type.GetInterface(nameof(ICollection)) != null);

        public static Type CollectionItemType(Type collectionType) => collectionType.GetGenericArguments().Single();
    }
}
