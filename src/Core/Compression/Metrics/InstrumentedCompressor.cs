using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Decorator that instruments any <see cref="ICompressor"/> with OpenTelemetry metrics.
    /// </summary>
    public class InstrumentedCompressor : ICompressor
    {
        private readonly ICompressor _inner;
        private readonly string _algorithmName;

        public InstrumentedCompressor(ICompressor inner, string? algorithmName = null)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _algorithmName = algorithmName ?? inner.GetType().Name.Replace("Compressor", "");
        }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            var sw = Stopwatch.StartNew();
            byte[] compressed = _inner.Compress(data);
            sw.Stop();

            CompressionMetrics.RecordCompression(_algorithmName, data.Length, compressed.Length, sw.Elapsed.TotalMilliseconds);
            return compressed;
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            var sw = Stopwatch.StartNew();
            byte[] decompressed = _inner.Decompress(data);
            sw.Stop();

            CompressionMetrics.RecordDecompression(_algorithmName, data.Length, decompressed.Length, sw.Elapsed.TotalMilliseconds);
            return decompressed;
        }

        public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            var sw = Stopwatch.StartNew();
            byte[] compressed = await _inner.CompressAsync(data, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            CompressionMetrics.RecordCompression(_algorithmName, data.Length, compressed.Length, sw.Elapsed.TotalMilliseconds);
            return compressed;
        }

        public async Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            var sw = Stopwatch.StartNew();
            byte[] decompressed = await _inner.DecompressAsync(data, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            CompressionMetrics.RecordDecompression(_algorithmName, data.Length, decompressed.Length, sw.Elapsed.TotalMilliseconds);
            return decompressed;
        }

        public async Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            long startSourcePos = source.CanSeek ? source.Position : 0;
            long startDestPos = destination.CanSeek ? destination.Position : 0;

            var sw = Stopwatch.StartNew();
            await _inner.CompressAsync(source, destination, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            if (source.CanSeek && destination.CanSeek)
            {
                long originalBytes = source.Position - startSourcePos;
                long compressedBytes = destination.Position - startDestPos;
                CompressionMetrics.RecordCompression(_algorithmName, originalBytes, compressedBytes, sw.Elapsed.TotalMilliseconds);
            }
        }

        public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            long startSourcePos = source.CanSeek ? source.Position : 0;
            long startDestPos = destination.CanSeek ? destination.Position : 0;

            var sw = Stopwatch.StartNew();
            await _inner.DecompressAsync(source, destination, cancellationToken).ConfigureAwait(false);
            sw.Stop();

            if (source.CanSeek && destination.CanSeek)
            {
                long compressedBytes = source.Position - startSourcePos;
                long decompressedBytes = destination.Position - startDestPos;
                CompressionMetrics.RecordDecompression(_algorithmName, compressedBytes, decompressedBytes, sw.Elapsed.TotalMilliseconds);
            }
        }
    }
}
