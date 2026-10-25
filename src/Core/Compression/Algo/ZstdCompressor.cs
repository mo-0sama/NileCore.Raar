using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;
using ZstdSharp;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Zstandard compression implementation using ZstdSharp.
    /// </summary>
    public class ZstdCompressor : ICompressor
    {
        private readonly int _level;

        public ZstdCompressor() : this(3) { }

        public ZstdCompressor(int level)
        {
            _level = level;
        }

        public ZstdCompressor(IOptions<CompressionOptions> options)
            : this(options?.Value?.ZstdLevel ?? 3) { }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var compressor = new Compressor(_level);
            Span<byte> compressedSpan = compressor.Wrap(data);

            byte[] compressedBytes = compressedSpan.ToArray();

            byte[] result = new byte[8 + compressedBytes.Length];
            BitConverter.GetBytes(data.Length).CopyTo(result, 0);
            BitConverter.GetBytes(compressedBytes.Length).CopyTo(result, 4);
            Buffer.BlockCopy(compressedBytes, 0, result, 8, compressedBytes.Length);

            return result;
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();
            if (data.Length < 8) throw new InvalidOperationException("Invalid Zstd payload format");

            int originalLength = BitConverter.ToInt32(data, 0);
            int compressedLength = BitConverter.ToInt32(data, 4);

            if (compressedLength != data.Length - 8)
                throw new InvalidOperationException("Invalid Zstd payload length");

            byte[] compressedBytes = new byte[compressedLength];
            Buffer.BlockCopy(data, 8, compressedBytes, 0, compressedLength);

            using var decompressor = new Decompressor();
            Span<byte> decompressedSpan = decompressor.Unwrap(compressedBytes);

            byte[] result = decompressedSpan.ToArray();

            if (result.Length != originalLength)
                throw new InvalidOperationException("Zstd decompression size mismatch");

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

            using var zstdStream = new CompressionStream(destination, _level, leaveOpen: true);
            await source.CopyToAsync(zstdStream, 81920, cancellationToken).ConfigureAwait(false);
        }

        public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var zstdStream = new DecompressionStream(source, leaveOpen: true);
            await zstdStream.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
        }
    }
}