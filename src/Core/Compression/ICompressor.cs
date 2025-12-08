namespace NileCore.Raar.Core.Compression
{
    public interface ICompressor
    {
        byte[] Compress(byte[] data);
        byte[] Decompress(byte[] data);
    }
}