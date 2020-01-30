using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AARC.Utilities
{
    public static class Extensions
    {
        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
#if NET60
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));
#else
            if (source == null) throw new ArgumentNullException("source");
            if (action == null) throw new ArgumentNullException("action");
#endif
            foreach (T item in source)
            {
                action(item);
            }
        }

        public static void WritePropertyValuesToConsole<T>(this T obj)
        {
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Console.Write("{0},", prop.GetValue(obj, null));
            }
            Console.WriteLine("#");
        }

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
            sb.Remove(sb.Length - 1, 1);    // remove the last comma  TODO: better sb.Length -= 1 ???
            sb.AppendLine();

            return sb.ToString();
        }

        public static void WritePropertyNamesToConsole<T>(this T obj)
        {
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                Console.Write("{0},", prop.Name);
            }
            Console.WriteLine("#");
        }

        public static string GetPropertyNames<T>(this T obj)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                sb.AppendFormat("{0},", prop.Name);
            }
            sb.Remove(sb.Length - 1, 1);    // remove the last comma
            sb.AppendLine();

            return sb.ToString();
        }

        public static string ToCsv<T>(this IEnumerable<T> objects)
        {
            StringBuilder sb = new StringBuilder();
            foreach (object o in objects)
            {
                //foreach (var prop in o.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
                //    sb.AppendFormat("{0},", prop.GetValue(o, null));
                sb.AppendFormat("{0},", o.ToString());
            }
            sb.Remove(sb.Length - 1, 1); // remove the last comma

            return sb.ToString();
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
    }
}