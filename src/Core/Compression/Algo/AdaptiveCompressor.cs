using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Adaptive compressor that automatically bypasses compression for small payloads below a threshold
    /// (where compression overhead exceeds space savings), and delegates to an inner compressor for larger payloads.
    /// </summary>
    public class AdaptiveCompressor : ICompressor
    {
        private const byte MarkerUncompressed = 0x00;
        private const byte MarkerCompressed = 0x01;

        private readonly ICompressor _innerCompressor;
        private readonly int _thresholdBytes;

        /// <summary>
        /// Initializes a new instance of <see cref="AdaptiveCompressor"/>.
        /// </summary>
        /// <param name="innerCompressor">The inner compressor to use for payloads reaching or exceeding the threshold.</param>
        /// <param name="thresholdBytes">Payload byte size threshold below which compression is bypassed. Default is 256 bytes.</param>
        public AdaptiveCompressor(ICompressor innerCompressor, int thresholdBytes = 256)
        {
            _innerCompressor = innerCompressor ?? throw new ArgumentNullException(nameof(innerCompressor));
            _thresholdBytes = thresholdBytes > 0 ? thresholdBytes : 256;
        }

        public byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            if (data.Length < _thresholdBytes)
            {
                byte[] uncompressedWithHeader = new byte[data.Length + 1];
                uncompressedWithHeader[0] = MarkerUncompressed;
                Buffer.BlockCopy(data, 0, uncompressedWithHeader, 1, data.Length);
                return uncompressedWithHeader;
            }

            byte[] compressed = _innerCompressor.Compress(data);
            byte[] result = new byte[compressed.Length + 1];
            result[0] = MarkerCompressed;
            Buffer.BlockCopy(compressed, 0, result, 1, compressed.Length);
            return result;
        }

        public byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            byte marker = data[0];
            int payloadLength = data.Length - 1;

            if (marker == MarkerUncompressed)
            {
                byte[] raw = new byte[payloadLength];
                Buffer.BlockCopy(data, 1, raw, 0, payloadLength);
                return raw;
            }
            else if (marker == MarkerCompressed)
            {
                byte[] compressedPayload = new byte[payloadLength];
                Buffer.BlockCopy(data, 1, compressedPayload, 0, payloadLength);
                return _innerCompressor.Decompress(compressedPayload);
            }

            throw new InvalidOperationException($"Invalid adaptive compression marker: 0x{marker:X2}");
        }

        public async Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            if (data.Length < _thresholdBytes)
            {
                byte[] uncompressedWithHeader = new byte[data.Length + 1];
                uncompressedWithHeader[0] = MarkerUncompressed;
                Buffer.BlockCopy(data, 0, uncompressedWithHeader, 1, data.Length);
                return uncompressedWithHeader;
            }

            byte[] compressed = await _innerCompressor.CompressAsync(data, cancellationToken).ConfigureAwait(false);
            byte[] result = new byte[compressed.Length + 1];
            result[0] = MarkerCompressed;
            Buffer.BlockCopy(compressed, 0, result, 1, compressed.Length);
            return result;
        }

        public async Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0) return Array.Empty<byte>();

            byte marker = data[0];
            int payloadLength = data.Length - 1;

            if (marker == MarkerUncompressed)
            {
                byte[] raw = new byte[payloadLength];
                Buffer.BlockCopy(data, 1, raw, 0, payloadLength);
                return raw;
            }
            else if (marker == MarkerCompressed)
            {
                byte[] compressedPayload = new byte[payloadLength];
                Buffer.BlockCopy(data, 1, compressedPayload, 0, payloadLength);
                return await _innerCompressor.DecompressAsync(compressedPayload, cancellationToken).ConfigureAwait(false);
            }

            throw new InvalidOperationException($"Invalid adaptive compression marker: 0x{marker:X2}");
        }

        public async Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var ms = new MemoryStream();
            await source.CopyToAsync(ms, 81920, cancellationToken).ConfigureAwait(false);
            byte[] compressed = await CompressAsync(ms.ToArray(), cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(compressed, 0, compressed.Length, cancellationToken).ConfigureAwait(false);
        }

        public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (destination == null) throw new ArgumentNullException(nameof(destination));

            using var ms = new MemoryStream();
            await source.CopyToAsync(ms, 81920, cancellationToken).ConfigureAwait(false);
            byte[] decompressed = await DecompressAsync(ms.ToArray(), cancellationToken).ConfigureAwait(false);
            await destination.WriteAsync(decompressed, 0, decompressed.Length, cancellationToken).ConfigureAwait(false);
        }
    }
}
