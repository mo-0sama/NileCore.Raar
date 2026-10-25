using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NileCore.Raar.Core.Compression;
using NileCore.Raar.Core.Json;
using NileCore.Raar.Net;
using Xunit;

namespace NileCore.Raar.Core.Tests
{
    public class JsonAndNetTests
    {
        public class SampleModel
        {
            public int Id { get; set; } = 123;
            public string Title { get; set; } = "High Performance JSON Compression";
            public string[] Tags { get; set; } = new[] { "json", "gzip", "brotli" };
        }

        [Fact]
        public void CompressJson_And_DecompressJson_RoundTrip_Succeeds()
        {
            var compressor = new GzipCompressor();
            var model = new SampleModel { Id = 555, Title = "Sync Test" };

            byte[] compressed = compressor.CompressJson(model);
            Assert.NotNull(compressed);

            var decompressed = compressor.DecompressJson<SampleModel>(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(555, decompressed!.Id);
            Assert.Equal("Sync Test", decompressed.Title);
        }

        [Fact]
        public async Task CompressJsonAsync_And_DecompressJsonAsync_RoundTrip_Succeeds()
        {
            var compressor = new BrotliCompressor();
            var model = new SampleModel { Id = 777, Title = "Async Test" };

            byte[] compressed = await compressor.CompressJsonAsync(model);
            Assert.NotNull(compressed);

            var decompressed = await compressor.DecompressJsonAsync<SampleModel>(compressed);
            Assert.NotNull(decompressed);
            Assert.Equal(777, decompressed!.Id);
            Assert.Equal("Async Test", decompressed.Title);
        }

        [Fact]
        public async Task Stream_CompressJsonAsync_And_DecompressJsonAsync_RoundTrip_Succeeds()
        {
            var compressor = new Lz4Compressor();
            var model = new SampleModel { Id = 888, Title = "Stream Test" };

            using var memStream = new MemoryStream();
            await compressor.CompressJsonAsync(model, memStream);
            memStream.Position = 0;

            var decompressed = await compressor.DecompressJsonAsync<SampleModel>(memStream);
            Assert.NotNull(decompressed);
            Assert.Equal(888, decompressed!.Id);
            Assert.Equal("Stream Test", decompressed.Title);
        }

        [Fact]
        public async Task CompressedHttpContent_Encodes_Content_Properly()
        {
            var compressor = new GzipCompressor();
            var rawJson = "{\"message\":\"Hello via CompressedHttpContent\"}";
            var rawContent = new StringContent(rawJson, Encoding.UTF8, "application/json");

            using var compressedContent = new CompressedHttpContent(rawContent, compressor, "gzip");

            Assert.Contains("gzip", compressedContent.Headers.ContentEncoding);

            using var memStream = new MemoryStream();
            await compressedContent.CopyToAsync(memStream);
            memStream.Position = 0;

            byte[] compressedBytes = memStream.ToArray();
            byte[] decompressedBytes = compressor.Decompress(compressedBytes);
            string decompressedString = Encoding.UTF8.GetString(decompressedBytes);

            Assert.Equal(rawJson, decompressedString);
        }

        [Fact]
        public void JsonExtensions_ToJson_FromJson_DeepClone_Work_Correctly()
        {
            var original = new SampleModel { Id = 4321, Title = "Original Title" };

            // 1. ToJson
            string json = original.ToJson();
            Assert.Contains("\"Id\":4321", json);

            // 2. FromJson
            var deserialized = json.FromJson<SampleModel>();
            Assert.NotNull(deserialized);
            Assert.Equal(4321, deserialized!.Id);
            Assert.Equal("Original Title", deserialized.Title);

            // 3. DeepClone
            var clone = original.DeepClone();
            Assert.NotNull(clone);
            Assert.NotSame(original, clone);
            Assert.Equal(original.Id, clone!.Id);
            Assert.Equal(original.Title, clone.Title);

            // Verify mutation on clone does not affect original
            clone.Title = "Mutated";
            Assert.Equal("Original Title", original.Title);
        }
    }
}
