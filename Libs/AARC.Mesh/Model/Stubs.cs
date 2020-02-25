using System;
using System.Collections.Generic;

namespace AARC.Mesh.Model
{
    public static class Stubs
    {
        public static IEnumerable<(TSource, TSource)> Changes2<TSource>(this IEnumerable<TSource> source, int count)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            foreach (var i in source)
                yield return (i, i);
        }
    }
}
