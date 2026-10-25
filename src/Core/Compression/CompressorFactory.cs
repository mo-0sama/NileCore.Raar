using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Factory for resolving configured and specific compression implementations.
    /// </summary>
    public class CompressorFactory
    {
        private readonly CompressionType _configAlgorithm;
        private readonly IServiceProvider _provider;

        public CompressorFactory(IOptions<CompressionOptions> options, IServiceProvider provider)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _configAlgorithm = options?.Value?.Algorithm ?? CompressionType.None;
        }

        /// <summary>
        /// Gets the configured default compressor.
        /// </summary>
        public ICompressor GetCompressor() => GetCompressor(_configAlgorithm);

        /// <summary>
        /// Gets a compressor for the specified algorithm.
        /// </summary>
        public ICompressor GetCompressor(CompressionType type) =>
            type switch
            {
                CompressionType.None => _provider.GetRequiredService<NoneCompressor>(),
                CompressionType.Brotli => _provider.GetRequiredService<BrotliCompressor>(),
                CompressionType.LZ4 => _provider.GetRequiredService<Lz4Compressor>(),
                CompressionType.Gzip => _provider.GetRequiredService<GzipCompressor>(),
                CompressionType.Zstd => _provider.GetRequiredService<ZstdCompressor>(),
                CompressionType.Deflate => _provider.GetRequiredService<DeflateCompressor>(),
                _ => throw new NotSupportedException($"Unsupported compression type: {type}")
            };
    }
}