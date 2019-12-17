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
                return BitConverter.GetBytes((UInt32)0);
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

            var gZipBuffer = new byte[compressedData.Length + 4];
            Buffer.BlockCopy(compressedData, 0, gZipBuffer, 4, compressedData.Length);
            Buffer.BlockCopy(BitConverter.GetBytes(buffer.Length), 0, gZipBuffer, 0, 4);
            return gZipBuffer;
        }

        public static string DecompressString(byte[] gZipBuffer, int index)
        {
            using (var memoryStream = new MemoryStream())
            {
                int dataLength = BitConverter.ToInt32(gZipBuffer, index);
                if (dataLength == 0)
                    return null;
                memoryStream.Write(gZipBuffer, index + 4, gZipBuffer.Length - 4 - index);

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
