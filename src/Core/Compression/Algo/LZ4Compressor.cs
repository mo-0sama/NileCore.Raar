using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using K4os.Compression.LZ4;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// LZ4 ultra-fast compression implementation using K4os.Compression.LZ4.
    /// </summary>
    public class Lz4Compressor : ICompressor
    {
        private readonly LZ4Level _level;

        public Lz4Compressor() : this(LZ4Level.L00_FAST) { }

        public Lz4Compressor(LZ4Level level)
        {
            _level = level;
        }

        public Lz4Compressor(IOptions<CompressionOptions> options)
            : this(options?.Value?.LZ4Level ?? LZ4Level.L00_FAST) { }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            int maxSize = LZ4Codec.MaximumOutputSize(data.Length);
            byte[] compressed = new byte[maxSize];

            int compressedLength = LZ4Codec.Encode(
                data, 0, data.Length,
                compressed, 0, compressed.Length,
                _level
            );

            byte[] result = new byte[4 + 4 + compressedLength];
            BitConverter.GetBytes(data.Length).CopyTo(result, 0);
            BitConverter.GetBytes(compressedLength).CopyTo(result, 4);
            Buffer.BlockCopy(compressed, 0, result, 8, compressedLength);

            return result;
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();
            if (data.Length < 8) throw new InvalidOperationException("Invalid LZ4 payload format");

            int originalLength = BitConverter.ToInt32(data, 0);
            int compressedLength = BitConverter.ToInt32(data, 4);

            if (compressedLength != (data.Length - 8))
                throw new InvalidOperationException("Invalid LZ4 payload length");

            byte[] result = new byte[originalLength];

            int decoded = LZ4Codec.Decode(
                data, 8, compressedLength,
                result, 0, originalLength
            );

            if (decoded != originalLength)
                throw new InvalidOperationException("LZ4 decompression failed");

            return result;
        }

        public Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Compress(data));
        }

        public Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Decompress(data));
        }

        public async Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var ms = new MemoryStream();
            await source.CopyToAsync(ms, 81920, cancellationToken).ConfigureAwait(false);
            byte[] compressed = Compress(ms.ToArray());
            await destination.WriteAsync(compressed, 0, compressed.Length, cancellationToken).ConfigureAwait(false);
        }

        public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var ms = new MemoryStream();
            await source.CopyToAsync(ms, 81920, cancellationToken).ConfigureAwait(false);
            byte[] decompressed = Decompress(ms.ToArray());
            await destination.WriteAsync(decompressed, 0, decompressed.Length, cancellationToken).ConfigureAwait(false);
        }
    }
}