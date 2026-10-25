using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NileCore.Raar.Abstractions.Compression;
using NileCore.Raar.Core.Compression;
using Xunit;

namespace NileCore.Raar.Core.Tests
{
    public class CompressionTests
    {
        private static readonly byte[] SampleData = Encoding.UTF8.GetBytes(
            "NileCore.Raar High-Performance Compression Test Suite! Testing all algorithms with repetitive text for high compressibility. " +
            "Repetitive Repetitive Repetitive Repetitive Repetitive 1234567890!@#$%^&*()");

        [Theory]
        [InlineData(CompressionType.None)]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.Gzip)]
        [InlineData(CompressionType.LZ4)]
        [InlineData(CompressionType.Zstd)]
        [InlineData(CompressionType.Deflate)]
        public void Compress_Decompress_Sync_RoundTrip_Succeeds(CompressionType type)
        {
            var compressor = CreateCompressor(type);

            byte[] compressed = compressor.Compress(SampleData);
            Assert.NotNull(compressed);

            byte[] decompressed = compressor.Decompress(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(SampleData, decompressed);
        }

        [Theory]
        [InlineData(CompressionType.None)]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.Gzip)]
        [InlineData(CompressionType.LZ4)]
        [InlineData(CompressionType.Zstd)]
        [InlineData(CompressionType.Deflate)]
        public async Task Compress_Decompress_Async_RoundTrip_Succeeds(CompressionType type)
        {
            var compressor = CreateCompressor(type);

            byte[] compressed = await compressor.CompressAsync(SampleData, CancellationToken.None);
            Assert.NotNull(compressed);

            byte[] decompressed = await compressor.DecompressAsync(compressed, CancellationToken.None);
            Assert.NotNull(decompressed);
            Assert.Equal(SampleData, decompressed);
        }

        [Theory]
        [InlineData(CompressionType.None)]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.Gzip)]
        [InlineData(CompressionType.LZ4)]
        [InlineData(CompressionType.Zstd)]
        [InlineData(CompressionType.Deflate)]
        public async Task Stream_Compress_Decompress_RoundTrip_Succeeds(CompressionType type)
        {
            var compressor = CreateCompressor(type);

            using var sourceStream = new MemoryStream(SampleData);
            using var compressedStream = new MemoryStream();

            await compressor.CompressAsync(sourceStream, compressedStream, CancellationToken.None);
            compressedStream.Position = 0;

            using var decompressedStream = new MemoryStream();
            await compressor.DecompressAsync(compressedStream, decompressedStream, CancellationToken.None);

            byte[] decompressedBytes = decompressedStream.ToArray();
            Assert.Equal(SampleData, decompressedBytes);
        }

        [Theory]
        [InlineData(CompressionType.None)]
        [InlineData(CompressionType.Brotli)]
        [InlineData(CompressionType.Gzip)]
        [InlineData(CompressionType.LZ4)]
        [InlineData(CompressionType.Zstd)]
        [InlineData(CompressionType.Deflate)]
        public void Compress_EmptyData_ReturnsEmptyArray(CompressionType type)
        {
            var compressor = CreateCompressor(type);
            var empty = Array.Empty<byte>();

            var compressed = compressor.Compress(empty);
            var decompressed = compressor.Decompress(compressed);

            Assert.Empty(decompressed);
        }

        [Fact]
        public void AdaptiveCompressor_Bypasses_Small_Payloads_And_Compresses_Large_Payloads()
        {
            var inner = new BrotliCompressor();
            var adaptive = new AdaptiveCompressor(inner, thresholdBytes: 100);

            // 1. Small payload (< 100 bytes)
            var smallData = Encoding.UTF8.GetBytes("Small payload");
            var smallCompressed = adaptive.Compress(smallData);
            Assert.Equal(smallData.Length + 1, smallCompressed.Length); // 1-byte marker + raw
            Assert.Equal(0x00, smallCompressed[0]); // uncompressed marker
            var smallDecompressed = adaptive.Decompress(smallCompressed);
            Assert.Equal(smallData, smallDecompressed);

            // 2. Large payload (>= 100 bytes)
            var largeCompressed = adaptive.Compress(SampleData);
            Assert.Equal(0x01, largeCompressed[0]); // compressed marker
            var largeDecompressed = adaptive.Decompress(largeCompressed);
            Assert.Equal(SampleData, largeDecompressed);
        }

        private static ICompressor CreateCompressor(CompressionType type) =>
            type switch
            {
                CompressionType.None => new NoneCompressor(),
                CompressionType.Brotli => new BrotliCompressor(),
                CompressionType.Gzip => new GzipCompressor(),
                CompressionType.LZ4 => new Lz4Compressor(),
                CompressionType.Zstd => new ZstdCompressor(),
                CompressionType.Deflate => new DeflateCompressor(),
                _ => throw new NotSupportedException()
            };
    }
}
