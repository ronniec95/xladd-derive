using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace AARC.Compression
{
    public static class Compression
    {
        public static byte[] CompressString(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return BitConverter.GetBytes((UInt64)0);
            }
            byte[] buffer = Encoding.UTF8.GetBytes(text);
            var memoryStream = new MemoryStream();
            using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress, true))
            {
                gZipStream.Write(buffer, 0, buffer.Length);
            }

            memoryStream.Position = 0;

            var compressedData = new byte[memoryStream.Length];
            memoryStream.Read(compressedData, 0, compressedData.Length);

            var gZipBuffer = new byte[compressedData.Length + sizeof(UInt64)];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, sizeof(UInt64), compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes((UInt64)buffer.Length), 0, gZipBuffer, 0, sizeof(UInt64));
            return gZipBuffer;
        }

        public static string DecompressString(byte[] gZipBuffer, int index)
        {
            using (var memoryStream = new MemoryStream())
            {
                var dataLength = BitConverter.ToUInt64(gZipBuffer, index);
                if (dataLength == 0)
                    return null;
                memoryStream.Write(gZipBuffer, sizeof(UInt64) + index, gZipBuffer.Length - sizeof(UInt64) - index);

                var buffer = new byte[dataLength];

                memoryStream.Position = 0;
                using (var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
                {
                    gZipStream.Read(buffer, 0, buffer.Length);
                }

                return Encoding.UTF8.GetString(buffer);
            }
        }
  #if NETSTANDARD2_1
        public static Stream Compress_Stream(Stream inputStream)
        {
            var outputStream = new MemoryStream();
            var compressor = new BrotliStream(outputStream, CompressionMode.Compress, true);
            inputStream.CopyTo(compressor);
            compressor.Dispose();
            return outputStream;
        }

        public static Stream Decompress_Stream(Stream inputStream)
        {
            var outputStream = new MemoryStream();
            var decompressor = new BrotliStream(inputStream, CompressionMode.Decompress, true);
            decompressor.CopyTo(outputStream);
            decompressor.Dispose();
            return outputStream;
        }
#endif
    }
}
