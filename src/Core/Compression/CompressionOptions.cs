using System.IO.Compression;
using K4os.Compression.LZ4;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Configuration options for compression services.
    /// </summary>
    public class CompressionOptions
    {
        /// <summary>
        /// Gets or sets the default compression algorithm to use. Default is <see cref="CompressionType.None"/>.
        /// </summary>
        public CompressionType Algorithm { get; set; } = CompressionType.None;

        /// <summary>
        /// Gets or sets the compression level for algorithms supporting <see cref="CompressionLevel"/> (Brotli, Gzip).
        /// Default is <see cref="CompressionLevel.Optimal"/>.
        /// </summary>
        public CompressionLevel Level { get; set; } = CompressionLevel.Optimal;

        /// <summary>
        /// Gets or sets the compression level for Zstandard (1 to 22). Default is 3.
        /// </summary>
        public int ZstdLevel { get; set; } = 3;

        /// <summary>
        /// Gets or sets the LZ4 compression level. Default is <see cref="LZ4Level.L00_FAST"/>.
        /// </summary>
        public LZ4Level LZ4Level { get; set; } = LZ4Level.L00_FAST;
    }

    /// <summary>
    /// Supported compression algorithms.
    /// </summary>
    public enum CompressionType
    {
        None,
        Brotli,
        LZ4,
        Gzip,
        Zstd,
        Deflate
    }
}