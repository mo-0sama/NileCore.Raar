using Microsoft.Extensions.DependencyInjection;
using NileCore.Raar.Abstractions.Compression;
using System;

namespace NileCore.Raar.Core.Compression
{
    public class CompressorFactory
    {
        private readonly CompressionType _configAlgorithm;
        private readonly CompressionOptions options;
        private readonly IServiceProvider provider;

        public CompressorFactory(CompressionOptions options, IServiceProvider provider)
        {
            this.options = options;
            this.provider = provider;
            _configAlgorithm = options.Algorithm;
        }

        public ICompressor GetCompressor() =>
            _configAlgorithm switch
            {
                CompressionType.None => provider.GetRequiredService<NoneCompressor>(),
                CompressionType.Brotli => provider.GetRequiredService<BrotliCompressor>(),
                CompressionType.LZ4 => provider.GetRequiredService<Lz4Compressor>(),
                CompressionType.Gzip => provider.GetRequiredService<GzipCompressor>(),
                CompressionType.Zstd => provider.GetRequiredService<ZstdCompressor>(),
                _ => throw new NotSupportedException("Unknown compression type")
            };
    }
}