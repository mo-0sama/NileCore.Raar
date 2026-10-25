using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Brotli compression implementation.
    /// </summary>
    public class BrotliCompressor : ICompressor
    {
        private readonly CompressionLevel _compressionLevel;

        public BrotliCompressor() : this(CompressionLevel.Optimal) { }

        public BrotliCompressor(CompressionLevel compressionLevel)
        {
            _compressionLevel = compressionLevel;
        }

        public BrotliCompressor(IOptions<CompressionOptions> options)
            : this(options?.Value?.Level ?? CompressionLevel.Optimal) { }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, _compressionLevel, leaveOpen: true))
            {
                brotli.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var input = new MemoryStream(data);
            using var brotli = new BrotliStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            brotli.CopyTo(output);
            return output.ToArray();
        }

        public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var output = new MemoryStream();
            using (var brotli = new BrotliStream(output, _compressionLevel, leaveOpen: true))
            {
                await brotli.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            }
            return output.ToArray();
        }

        public async Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var input = new MemoryStream(data);
            using var brotli = new BrotliStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await brotli.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
            return output.ToArray();
        }

        public async Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var brotli = new BrotliStream(destination, _compressionLevel, leaveOpen: true);
            await source.CopyToAsync(brotli, 81920, cancellationToken).ConfigureAwait(false);
        }

        public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var brotli = new BrotliStream(source, CompressionMode.Decompress, leaveOpen: true);
            await brotli.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
        }
    }
}