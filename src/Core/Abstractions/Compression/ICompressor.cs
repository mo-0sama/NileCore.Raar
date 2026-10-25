using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NileCore.Raar.Abstractions.Compression
{
    /// <summary>
    /// Provides compression and decompression abstractions for byte arrays and streams.
    /// </summary>
    public interface ICompressor
    {
        /// <summary>
        /// Compresses the input byte array synchronously.
        /// </summary>
        /// <param name="data">The raw uncompressed data.</param>
        /// <returns>The compressed byte array.</returns>
        byte[] Compress(byte[] data);

        /// <summary>
        /// Decompresses the input byte array synchronously.
        /// </summary>
        /// <param name="data">The compressed data.</param>
        /// <returns>The decompressed raw byte array.</returns>
        byte[] Decompress(byte[] data);

        /// <summary>
        /// Compresses the input byte array asynchronously.
        /// </summary>
        /// <param name="data">The raw uncompressed data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation that yields the compressed byte array.</returns>
        Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Compress(data));
        }

        /// <summary>
        /// Decompresses the input byte array asynchronously.
        /// </summary>
        /// <param name="data">The compressed data.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>A task representing the asynchronous operation that yields the decompressed raw byte array.</returns>
        Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Decompress(data));
        }

        /// <summary>
        /// Compresses data from a source stream directly into a destination stream asynchronously.
        /// </summary>
        /// <param name="source">The source uncompressed stream.</param>
        /// <param name="destination">The destination stream where compressed data is written.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default);

        /// <summary>
        /// Decompresses data from a source stream directly into a destination stream asynchronously.
        /// </summary>
        /// <param name="source">The source compressed stream.</param>
        /// <param name="destination">The destination stream where decompressed data is written.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default);
    }
}