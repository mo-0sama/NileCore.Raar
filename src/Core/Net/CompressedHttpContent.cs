using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Net
{
    /// <summary>
    /// An <see cref="HttpContent"/> wrapper that transparently compresses content using an <see cref="ICompressor"/>.
    /// </summary>
    public class CompressedHttpContent : HttpContent
    {
        private readonly HttpContent _originalContent;
        private readonly ICompressor _compressor;
        private readonly string _encodingName;

        public CompressedHttpContent(HttpContent originalContent, ICompressor compressor, string encodingName)
        {
            _originalContent = originalContent ?? throw new ArgumentNullException(nameof(originalContent));
            _compressor = compressor ?? throw new ArgumentNullException(nameof(compressor));
            _encodingName = encodingName ?? throw new ArgumentNullException(nameof(encodingName));

            foreach (var header in _originalContent.Headers)
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            Headers.ContentEncoding.Add(_encodingName);
        }

        protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
        {
            using var rawStream = new MemoryStream();
            await _originalContent.CopyToAsync(rawStream).ConfigureAwait(false);
            rawStream.Position = 0;
            await _compressor.CompressAsync(rawStream, stream, CancellationToken.None).ConfigureAwait(false);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = -1;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _originalContent.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    /// <summary>
    /// Stream compression extensions for network streams.
    /// </summary>
    public static class StreamCompressionExtensions
    {
        /// <summary>
        /// Reads all bytes from a source stream, compresses them using the compressor, and writes to destination.
        /// </summary>
        public static Task CompressStreamAsync(
            this ICompressor compressor,
            Stream source,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            return compressor.CompressAsync(source, destination, cancellationToken);
        }

        /// <summary>
        /// Reads all compressed bytes from a source stream, decompresses them, and writes to destination.
        /// </summary>
        public static Task DecompressStreamAsync(
            this ICompressor compressor,
            Stream source,
            Stream destination,
            CancellationToken cancellationToken = default)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            return compressor.DecompressAsync(source, destination, cancellationToken);
        }
    }
}
