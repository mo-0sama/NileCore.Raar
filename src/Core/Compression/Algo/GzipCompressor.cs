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
    /// GZip compression implementation.
    /// </summary>
    public class GzipCompressor : ICompressor
    {
        private readonly CompressionLevel _compressionLevel;

        public GzipCompressor() : this(CompressionLevel.Optimal) { }

        public GzipCompressor(CompressionLevel compressionLevel)
        {
            _compressionLevel = compressionLevel;
        }

        public GzipCompressor(IOptions<CompressionOptions> options)
            : this(options?.Value?.Level ?? CompressionLevel.Optimal) { }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, _compressionLevel, leaveOpen: true))
            {
                gzip.Write(data, 0, data.Length);
            }
            return output.ToArray();
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, _compressionLevel, leaveOpen: true))
            {
                await gzip.WriteAsync(data, 0, data.Length, cancellationToken).ConfigureAwait(false);
            }
            return output.ToArray();
        }

        public async Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            using var input = new MemoryStream(data);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            await gzip.CopyToAsync(output, 81920, cancellationToken).ConfigureAwait(false);
            return output.ToArray();
        }

        public async Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var gzip = new GZipStream(destination, _compressionLevel, leaveOpen: true);
            await source.CopyToAsync(gzip, 81920, cancellationToken).ConfigureAwait(false);
        }

        public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var gzip = new GZipStream(source, CompressionMode.Decompress, leaveOpen: true);
            await gzip.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
        }
    }
}
