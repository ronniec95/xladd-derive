using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using AARC.Utilities;

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

            if (string.IsNullOrEmpty(str))
            {
                bytes.AddRange(BitConverter.GetBytes((UInt32)0));
                return bytes;
            }
            bytes.AddRange(BitConverter.GetBytes((UInt32)str.Length));
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
            // Shame I have to convert UInt32 for a string len to Int32
            var ulen = BitConverter.ToUInt32(bytes, msgPtr);
            var len = (Int32)(ulen);
            msgPtr += sizeof(UInt32);
            if (len == 0)
                return string.Empty;

            var decodedString =  System.Text.Encoding.ASCII.GetString(bytes, msgPtr, len);
            msgPtr += len;
            return decodedString;
        }

        public static UInt32 ToUInt32(this byte[] bytes, ref int msgPtr)
        {
            var value = BitConverter.ToUInt32(bytes, msgPtr);
            msgPtr += sizeof(UInt32);
            return value;
        }

        public static UInt64 ToUInt64(this byte[] bytes, ref int msgPtr)
        {
            var value = BitConverter.ToUInt64(bytes, msgPtr);
            msgPtr += sizeof(UInt64);
            return value;
        }

        public static byte[] CloneReduce(this byte[] byteArray, int len, int index = 0)
        {
            byte[] tmp = new byte[len];
            Array.ConstrainedCopy(byteArray, 0, tmp, 0, len);

            return tmp;
        }

        public static IEnumerable<T> Remove<T>(this IEnumerable<T> enumerable, int index)
        {
            int current = 0;
            foreach (var item in enumerable)
            {
                if (current != index)
                    yield return item;

                current++;
            }
        }

        public static void UpdateNT(DateTime now, byte[] bytes, int offset = 0)
        {
            var (totalSeconds, milliseconds) = DateTimeUtilities.DateTimeToUnixTotalSeconds(now);
            var b = BitConverter.GetBytes(totalSeconds);

            for (var i = 0; i < b.Length; ++i)
                bytes[i + offset] = b[i];

            b = BitConverter.GetBytes(milliseconds);
            for (var i = 0; i < b.Length; ++i)
                bytes[i + offset + sizeof(UInt64)] = b[i];
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

        public static string GetLocalHostFQDN()
        {
            string hostname;
            if (WhichOS.IsMacOS)
            {
                var ipProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                var domain = ipProperties.DomainName;
                hostname = ipProperties.HostName;
                if (hostname.EndsWith(".local"))
                    return hostname;

                if (string.IsNullOrEmpty(domain))
                    return $"{hostname}.local";
            }
                hostname = $"{Dns.GetHostName()}";

            return hostname;

        }
    }
}
