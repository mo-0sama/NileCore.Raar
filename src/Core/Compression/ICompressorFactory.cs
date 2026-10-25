using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Defines a factory for resolving default, typed, and named compression profile implementations.
    /// </summary>
    public interface ICompressorFactory
    {
        /// <summary>
        /// Gets the configured default compressor.
        /// </summary>
        ICompressor GetCompressor();

        /// <summary>
        /// Gets a compressor for the specified algorithm.
        /// </summary>
        ICompressor GetCompressor(CompressionType type);

        /// <summary>
        /// Gets a compressor configured with the specified named profile.
        /// </summary>
        ICompressor GetCompressor(string profileName);
    }
}
