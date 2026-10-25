using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;
using NileCore.Raar.Core.Compression;
using Xunit;

namespace NileCore.Raar.Core.Tests
{
    public class CompressorFactoryTests
    {
        [Theory]
        [InlineData(CompressionType.None, typeof(NoneCompressor))]
        [InlineData(CompressionType.Brotli, typeof(BrotliCompressor))]
        [InlineData(CompressionType.Gzip, typeof(GzipCompressor))]
        [InlineData(CompressionType.LZ4, typeof(Lz4Compressor))]
        [InlineData(CompressionType.Zstd, typeof(ZstdCompressor))]
        public void Factory_Resolves_Correct_Algorithm_Type(CompressionType type, Type expectedType)
        {
            var services = new ServiceCollection();
            services.AddRaarCompression(o => o.Algorithm = type);
            var sp = services.BuildServiceProvider();

            var factory = sp.GetRequiredService<CompressorFactory>();
            var compressor = factory.GetCompressor(type);

            Assert.IsType(expectedType, compressor);
        }

        [Fact]
        public void Factory_GetCompressor_Default_Returns_Configured_Algorithm()
        {
            var services = new ServiceCollection();
            services.AddRaarCompression(o => o.Algorithm = CompressionType.Brotli);
            var sp = services.BuildServiceProvider();

            var factory = sp.GetRequiredService<CompressorFactory>();
            var defaultCompressor = factory.GetCompressor();

            Assert.IsType<BrotliCompressor>(defaultCompressor);
        }
    }
}
