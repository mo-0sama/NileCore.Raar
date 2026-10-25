using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Json
{
    /// <summary>
    /// Extension methods for JSON serialization combined with high-performance compression.
    /// </summary>
    public static class JsonCompressionExtensions
    {
        /// <summary>
        /// Serializes an object to JSON and compresses it using the specified compressor.
        /// </summary>
        public static byte[] CompressJson<T>(this ICompressor compressor, T value, JsonSerializerOptions? options = null)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            byte[] utf8Bytes = JsonSerializer.SerializeToUtf8Bytes(value, options);
            return compressor.Compress(utf8Bytes);
        }

        /// <summary>
        /// Decompresses data using the specified compressor and deserializes the JSON to the target type.
        /// </summary>
        public static T? DecompressJson<T>(this ICompressor compressor, byte[] data, JsonSerializerOptions? options = null)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            if (data == null || data.Length == 0) return default;
            byte[] decompressedBytes = compressor.Decompress(data);
            return JsonSerializer.Deserialize<T>(decompressedBytes, options);
        }

        /// <summary>
        /// Asynchronously serializes an object to JSON and compresses it using the specified compressor.
        /// </summary>
        public static async Task<byte[]> CompressJsonAsync<T>(
            this ICompressor compressor,
            T value,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            using var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, value, options, cancellationToken).ConfigureAwait(false);
            byte[] utf8Bytes = ms.ToArray();
            return await compressor.CompressAsync(utf8Bytes, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously decompresses data and deserializes the JSON to the target type.
        /// </summary>
        public static async Task<T?> DecompressJsonAsync<T>(
            this ICompressor compressor,
            byte[] data,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            if (data == null || data.Length == 0) return default;
            byte[] decompressedBytes = await compressor.DecompressAsync(data, cancellationToken).ConfigureAwait(false);
            using var ms = new MemoryStream(decompressedBytes);
            return await JsonSerializer.DeserializeAsync<T>(ms, options, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Serializes and compresses an object directly into a destination stream asynchronously.
        /// </summary>
        public static async Task CompressJsonAsync<T>(
            this ICompressor compressor,
            T value,
            Stream destination,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var rawStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(rawStream, value, options, cancellationToken).ConfigureAwait(false);
            rawStream.Position = 0;
            await compressor.CompressAsync(rawStream, destination, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Decompresses and deserializes JSON from a source stream asynchronously.
        /// </summary>
        public static async Task<T?> DecompressJsonAsync<T>(
            this ICompressor compressor,
            Stream source,
            JsonSerializerOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            if (source == null) throw new ArgumentNullException(nameof(source));

            using var decompressedStream = new MemoryStream();
            await compressor.DecompressAsync(source, decompressedStream, cancellationToken).ConfigureAwait(false);
            decompressedStream.Position = 0;
            return await JsonSerializer.DeserializeAsync<T>(decompressedStream, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
