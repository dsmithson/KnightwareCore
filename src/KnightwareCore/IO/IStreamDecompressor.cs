namespace Knightware.IO
{
    /// <summary>
    /// Decompresses a provided zip stream
    /// </summary>
    public interface IStreamDecompressor
    {
        byte[] Decompress(byte[] zipCompressedData, int offset, int count, int uncompressedDataLength);
    }
}
