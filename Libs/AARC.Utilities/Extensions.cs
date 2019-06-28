using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace AARC.Utilities
{
    public static class Extensions
    {
        public static double AsDouble(this DateTime date)
        {
            return DateTimeUtilities.DateTimeToUnixTimestamp(date);
        }

        /// <summary>
        /// Using Linq and an internal q to calculate a moving sum
        /// </summary>
        /// <param name="source"></param>
        /// <param name="sampleLength"></param>
        /// <returns></returns>
        public static IEnumerable<double> MovingSum(this IEnumerable<double> source, int sampleLength)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sampleLength <= 0) throw new ArgumentException("Invalid sample length");

            return MovingSumImp(source, sampleLength);
        }

        private static IEnumerable<double> MovingSumImp(IEnumerable<double> source, int length)
        {
            Queue<double> sample = new Queue<double>(length);

            foreach (double d in source)
            {
                if (sample.Count == length)
                {
                    sample.Dequeue();
                }
                sample.Enqueue(d);
                yield return sample.Sum() / sample.Count * length;
            }
        }
        /// <summary>
        ///  Ex: collection.TakeLast(5);
        /// </summary>
        /// <typeparam name="T">type of collection</typeparam>
        /// <param name="source">collect</param>
        /// <param name="n">Last number of items required</param>
        /// <returns></returns>
        public static IEnumerable<T> TakeLast<T>(this IEnumerable<T> source, int n)
        {
            IEnumerable<T> enumerable = source as T[] ?? source.ToArray();
            return enumerable.Skip(Math.Max(0, enumerable.Count() - n));
        }

        public static IEnumerable<IEnumerable<TSource>> Batch<TSource>(this IEnumerable<TSource> source, int batchSize)
        {
            var batch = new List<TSource>();
            foreach (var item in source)
            {
                batch.Add(item);
                if (batch.Count == batchSize)
                {
                    yield return batch;
                    batch = new List<TSource>();
                }
            }

            if (batch.Any()) yield return batch;
        }

        public static void AddRange<T>(this ConcurrentBag<T> @this, IEnumerable<T> toAdd)
        {
            foreach (var element in toAdd)
            {
                @this.Add(element);
            }
        }

        public static int IndexWhere<T>(this IEnumerable<T> data, Func<T, bool> whereClause)
        {
            var indices = data.IndexesWhere(whereClause).ToList();
            if (!indices.Any())
                return -1;

            return indices.First();
        }

        public static IEnumerable<int> IndexesWhere<T>(this IEnumerable<T> data, Func<T, bool> whereClause)
        {
            return data.Select((x, i) => new { Index = i, Value = x }).Where(x => whereClause(x.Value)).Select(x => x.Index);
        }

        public static int MaxIndex<T>(this IEnumerable<T> sequence) where T : IComparable<T>
        {
            int maxIndex = -1;
            T maxValue = default(T); // Immediately overwritten anyway

            int index = 0;
            foreach (T value in sequence)
            {
                if (value.CompareTo(maxValue) > 0 || maxIndex == -1)
                {
                    maxIndex = index;
                    maxValue = value;
                }
                index++;
            }
            return maxIndex;
        }

        public static int MaxIndexList<T>(this IList<T> elements) where T : IComparable<T>
        {
            int indexMax = !elements.Any() ? -1 : elements
                .Select((value, index) => new { Value = value, Index = index })
                .Aggregate((a, b) => (a.Value.CompareTo(b.Value) > 0) ? a : b)
                .Index;
            return indexMax;
        }

        public static int MaxIndexList<T>(this IList<T> elements, Func<T, T, int> comparator)
        {
            int indexMax = !elements.Any() ? -1 : elements
                .Select((value, index) => new { Value = value, Index = index })
                .Aggregate((a, b) => (comparator(a.Value, b.Value) > 0) ? a : b)
                .Index;
            return indexMax;
        }

        public static string FirstLetterToUpper(this string str)
        {
            if (str == null)
                return null;

            if (str.Length > 1)
                return char.ToUpper(str[0]) + str.Substring(1);

            return str.ToUpper();
        }

        public static IEnumerable<DateTime> Range(this DateTime startDate, DateTime endDate)
        {
            return Enumerable.Range(0, (endDate - startDate).Days + 1).Select(d => startDate.AddDays(d));
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T, int> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            int i = 0;
            foreach (var item in source)
            {
                action(item, i++);
            }
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));

            foreach (var item in source)
            {
                action(item);
            }
        }

        public static string GetPropertyNamesAndValues<T>(this T obj)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                object o = prop.GetValue(obj, null);
                string s = o is double ? Math.Round((double)o, 4).ToString(CultureInfo.InvariantCulture) : o.ToString();
                sb.Append($"{prop.Name} = {s},");
            }
            if (sb.Length > 0)
                sb.Length--;
            return sb.ToString();
        }

        //public static void WritePropertyValuesToConsole<T>(this T obj)
        //{
        //    Console.Write(obj.GetPropertyValues());
        //}

        //public static void WritePropertyNamesToConsole<T>(this T obj)
        //{
        //    Console.Write(obj.GetPropertyNames().ToCsv());
        //}

        // TODO: Reflection helper for the property values stuff?
        public static string GetPropertyValues<T>(this T obj)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                object o = prop.GetValue(obj, null);
                if (o is double)
                    sb.AppendFormat("{0:F4},", o);
                else
                    sb.AppendFormat("{0},", o);
            }
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1);    // remove the last comma
            sb.AppendLine();

            return sb.ToString();
        }

        public static void SetPropertyValue<T>(this T obj, string propertyName, object value)
        {
            PropertyInfo propertyInfo = obj.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
                return;

            //propertyInfo.SetValue(obj, Convert.ChangeType(value, propertyInfo.PropertyType));
            Type t = Nullable.GetUnderlyingType(propertyInfo.PropertyType) ?? propertyInfo.PropertyType;
            object safeValue = (value == null) ? null : Convert.ChangeType(value, t);
            propertyInfo.SetValue(obj, safeValue);
        }

        public static List<string> GetPropertyNames<T>(this T obj)
        {
            return obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public).Select(x => x.Name).ToList();
            //StringBuilder sb = new StringBuilder();
            //foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            //{
            //    sb.AppendFormat("{0},", prop.Name);
            //}
            //if (sb.Length > 0)
            //    sb.Remove(sb.Length - 1, 1);    // remove the last comma
            //sb.AppendLine();

            //return sb.ToString();
        }

        // faster when array is available
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToCsv(this string[] objects)
        {
            return string.Join(",", objects);
        }

        public static string ToCsvSj<T>(this IEnumerable<T> objects, string format)
        {
            string[] strings = objects.Select(s => string.Format(format, s)).ToArray();
            return string.Join(",", strings);
        }

        /// <summary>
        /// this seems faster (StringBuilder) when needing to format
        /// </summary>
        /// <param name="objects"></param>
        /// <param name="format">e.g. "{0:ddMMyy}" or "{0:F4}"</param>
        public static string ToCsv<T>(this IEnumerable<T> objects, string format)
        {
            //string[] strings = objects.Select(s => string.Format(format, s)).ToArray();
            //return string.Join(",", strings);

            StringBuilder sb = new StringBuilder();
            foreach (object o in objects)
            {
                sb.AppendFormat(format, o);
                sb.Append(",");
            }
            if (sb.Length > 0)
                sb.Remove(sb.Length - 1, 1); // remove the last comma

            return sb.ToString();
        }

        public static string ToCsv<T>(this IEnumerable<T> objects)
        {
            // faster than string builder... (no formatting needed)
            return string.Join(",", objects);
        }

        public static string ToCsvAnon<T>(this IEnumerable<T> objects)
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;
            foreach (object o in objects)
            {
                if (first)
                {
                    foreach (var prop in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                        sb.AppendFormat("{0},", prop.Name);
                    sb.Remove(sb.Length - 1, 1);    // remove the last comma
                    sb.AppendLine();
                    first = false;
                }

                foreach (var prop in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                    sb.AppendFormat("{0},", prop.GetValue(o, null));
                sb.Remove(sb.Length - 1, 1);    // remove the last comma
                sb.AppendLine();
            }

            return sb.ToString();
        }

        // ReSharper disable once InconsistentNaming
        public static string yyyMMdd(this DateTime date)
        {
            return date.ToString("yyyyMMdd");
        }

        /// <summary>
        /// If date is not friday got back until the last friday
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static DateTime FridayReverse(this DateTime date)
        {
            if (date.DayOfWeek == DayOfWeek.Friday)
                return date;
            if (date.DayOfWeek < DayOfWeek.Friday)
            {
                int dateOffset = (int)date.DayOfWeek + 2;
                date = date.AddDays(-dateOffset);
                return date;
            }
            else
            {
                int dateOffset = DayOfWeek.Friday - date.DayOfWeek;
                date = date.AddDays(dateOffset);
                return date;
            }
        }

        public static DateTime BusinessDay(this DateTime date, bool fwd)
        {
            if (fwd)
            {
                if (date.DayOfWeek == DayOfWeek.Saturday)
                    return date.AddDays(2);
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                    return date.AddDays(1);
            }
            else
            {
                if (date.DayOfWeek == DayOfWeek.Saturday)
                    return date.AddDays(-1);
                else if (date.DayOfWeek == DayOfWeek.Sunday)
                    return date.AddDays(-2);
            }
            return date;
        }
    }
}