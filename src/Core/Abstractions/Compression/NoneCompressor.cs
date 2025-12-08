namespace NileCore.Raar.Abstractions.Compression
{
    public class NoneCompressor : ICompressor
    {
        public byte[] Compress(byte[] data) => data;
        public byte[] Decompress(byte[] data) => data;
    }
}