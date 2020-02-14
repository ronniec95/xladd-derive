using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoreLinq;

namespace AARC.Mesh
{
    public static class Radix
    {
        public const string radixconst = "\rabcdefghijklmnopqrstuvwxyz0123456789._:/";

        public static string Decode(IList<UInt64> encoding)
        {
            var s = encoding.Select(Encode).Aggregate(string.Empty, (current, next) => current + next);
            return s;
        }

        public static List<UInt64> Encode(string name)
        {
            if (string.IsNullOrEmpty(name))
                return new List<UInt64>();

            var encodings = name.Batch(12)
                .Select(chunk => chunk.Aggregate(seed: (UInt64)0, func: (result, item) => result * (UInt64)radixconst.Length + (UInt64)radixconst.IndexOf(char.ToLower(item))));

            return encodings.ToList();
        }

        public static String Encode(UInt64 encoding)
        {
            var s = new StringBuilder();
            if (encoding > 0)
            {
                var value = encoding;
                while (value > 0)
                {
                    var i = value % (UInt64)radixconst.Length;
                    var r = radixconst[(int)i];
                    s.Insert(0, r);
                    value /= (UInt64)radixconst.Length;
                }
            }
            return s.ToString();
        }

        public static void RadixToBytes(List<byte> bytes, IList<UInt64> list)
        {
            bytes.Add((byte)list.Count);
            if (list.Count > 0)
            {
                var conversion = list.SelectMany(BitConverter.GetBytes);
                bytes.AddRange(conversion);
            }
        }

        public static List<UInt64> BytesToRadix(byte[] bytes, ref int msgPtr)
        {
            int ServiceLength = bytes[msgPtr++];
            var uints = new List<UInt64>();
            for (var i = 0; i < ServiceLength; i++)
                uints.Add(BitConverter.ToUInt64(bytes, msgPtr + i * sizeof(UInt64)));
            msgPtr += ServiceLength * sizeof(UInt64);

            return uints;
        }
    }
}
