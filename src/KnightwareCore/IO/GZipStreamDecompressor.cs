using System.IO;
using System.IO.Compression;

namespace Knightware.IO
{
    public class GZipStreamDecompressor : IGZipStreamDecompressor
    {
        public byte[] Decompress(byte[] zipCompressedData, int offset, int count, int uncompressedDataLength)
        {
            using (MemoryStream compressedStream = new MemoryStream(zipCompressedData, offset, count, false))
            {
                using (var decompressor = new GZipStream(compressedStream, CompressionMode.Decompress))
                {
                    byte[] decompressedBytes = new byte[uncompressedDataLength];
                    int read = decompressor.Read(decompressedBytes, 0, uncompressedDataLength);
                    return (read == uncompressedDataLength ? decompressedBytes : null);
                }
            }
        }
    }
}
