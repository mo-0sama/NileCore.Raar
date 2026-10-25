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
    /// Deflate compression implementation using <see cref="DeflateStream"/>.
    /// </summary>
    public class DeflateCompressor : ICompressor
    {
        private readonly CompressionLevel _compressionLevel;

        public DeflateCompressor() : this(CompressionLevel.Optimal) { }

        public DeflateCompressor(CompressionLevel compressionLevel)
        {
            _compressionLevel = compressionLevel;
        }

        public DeflateCompressor(IOptions<CompressionOptions> options)
            : this(options?.Value?.Level ?? CompressionLevel.Optimal) { }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var output = new MemoryStream();
            using (var deflate = new DeflateStream(output, _compressionLevel, leaveOpen: true))
            {
                deflate.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var input = new MemoryStream(data);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            return output.ToArray();
        }

        public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var output = new MemoryStream();
            using (var deflate = new DeflateStream(output, _compressionLevel, leaveOpen: true))
            {
                await deflate.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            }
            return output.ToArray();
        }

        public async Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var input = new MemoryStream(data);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await deflate.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
            return output.ToArray();
        }

        public async Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var deflate = new DeflateStream(destination, _compressionLevel, leaveOpen: true);
            await source.CopyToAsync(deflate, 81920, cancellationToken).ConfigureAwait(false);
        }

        public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var deflate = new DeflateStream(source, CompressionMode.Decompress, leaveOpen: true);
            await deflate.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
        }
    }
}
