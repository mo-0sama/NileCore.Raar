namespace NileCore.Raar.Core.Compression
{
    public class CompressionOptions
    {
        public CompressionType Algorithm { get; set; }
    }
    public enum CompressionType
    {
        None,
        Brotli,
        LZ4,
        Gzip,
        Zstd
    }
}