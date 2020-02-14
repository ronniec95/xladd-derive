using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace AARC.Mesh
{
    public static class MeshUtilities
    {
        public static string GetRandomString()
        {
            string path = System.IO.Path.GetRandomFileName();
            path = path.Replace(".", ""); // Remove period.
            return path;
        }

        /// <summary>
        /// Encode string to bytes as length + bytes encoded string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static IEnumerable<byte> EncodeBytes(this string str)
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(str.Length));
            bytes.AddRange(System.Text.Encoding.ASCII.GetBytes(str));
            return bytes;
        }

        /// <summary>
        /// Read bytes taking length (int32) and bytes to string encoding
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="msgPtr">position in byte stream to read from</param>
        /// <returns></returns>
        public static string DecodeString(this byte[] bytes, ref int msgPtr)
        {
            var len = BitConverter.ToInt32(bytes, msgPtr);
            msgPtr += sizeof(Int32);
            var decodedString =  System.Text.Encoding.ASCII.GetString(bytes, msgPtr, len);
            msgPtr += len;
            return decodedString;
        }

        public static byte[] CloneReduce(this byte[] byteArray, int len, int index = 0)
        {
            byte[] tmp = new byte[len];
            Array.ConstrainedCopy(byteArray, 0, tmp, 0, len);

            return tmp;
        }

        public static T Next<T>(this T v) where T : struct
        {
            return Enum.GetValues(v.GetType()).Cast<T>().Concat(new[] { default(T) }).SkipWhile(e => !v.Equals(e)).Skip(1).First();
        }

        public static T Previous<T>(this T v) where T : struct
        {
            return Enum.GetValues(v.GetType()).Cast<T>().Concat(new[] { default(T) }).Reverse().SkipWhile(e => !v.Equals(e)).Skip(1).First();
        }

        public static void Merge(ConcurrentDictionary<string, HashSet<string>> d, ConcurrentDictionary<string, HashSet<string>> s)
        {
            foreach(var kp in s)
            {
                if (d.ContainsKey(kp.Key))
                    d[kp.Key].UnionWith(kp.Value);
                else
                    d.TryAdd(kp.Key, kp.Value);
            }
        }

        public static uint NewXId => (uint)DateTime.Now.TimeOfDay.TotalMilliseconds;
    }
}
