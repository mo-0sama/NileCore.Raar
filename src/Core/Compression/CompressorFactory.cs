using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;
using System;

namespace NileCore.Raar.Core.Compression
{
    public class CompressorFactory
    {
        private readonly CompressionType _configAlgorithm;
        private readonly IServiceProvider provider;

        public CompressorFactory(IOptions<CompressionOptions> options, IServiceProvider provider)
        {
            this.provider = provider;
            _configAlgorithm = options?.Value.Algorithm ?? CompressionType.None;
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