using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NileCore.Raar.Abstractions.Compression
{
    /// <summary>
    /// Pass-through compressor that performs no compression.
    /// </summary>
    public class NoneCompressor : ICompressor
    {
        public byte[] Compress(byte[] data) => data;
        public byte[] Decompress(byte[] data) => data;

        public Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(data);
        }

        public Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(data);
        }

        public async Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            await source.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
        }

        public async Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default)
        {
            await source.CopyToAsync(destination, 81920, cancellationToken).ConfigureAwait(false);
        }
    }
}