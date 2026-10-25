using System;
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Factory for resolving default, typed, and named compression profile implementations.
    /// </summary>
    public class CompressorFactory : ICompressorFactory
    {
        private readonly IServiceProvider _provider;
        private readonly IOptionsMonitor<CompressionOptions>? _optionsMonitor;
        private readonly CompressionType _defaultAlgorithm;
        private readonly ConcurrentDictionary<string, ICompressor> _namedCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Initializes a new instance of <see cref="CompressorFactory"/>.
        /// </summary>
        public CompressorFactory(
            IOptions<CompressionOptions> options,
            IServiceProvider provider,
            IOptionsMonitor<CompressionOptions>? optionsMonitor = null)
        {
            _provider = provider ?? throw new ArgumentNullException(nameof(provider));
            _defaultAlgorithm = options?.Value?.Algorithm ?? CompressionType.None;
            _optionsMonitor = optionsMonitor;
        }

        /// <summary>
        /// Gets the configured default compressor.
        /// </summary>
        public ICompressor GetCompressor() => GetCompressor(_defaultAlgorithm);

        /// <summary>
        /// Gets a compressor for the specified algorithm.
        /// </summary>
        public ICompressor GetCompressor(CompressionType type) =>
            type switch
            {
                CompressionType.None => _provider.GetRequiredService<NoneCompressor>(),
                CompressionType.Brotli => _provider.GetRequiredService<BrotliCompressor>(),
                CompressionType.LZ4 => _provider.GetRequiredService<Lz4Compressor>(),
                CompressionType.Gzip => _provider.GetRequiredService<GzipCompressor>(),
                CompressionType.Zstd => _provider.GetRequiredService<ZstdCompressor>(),
                CompressionType.Deflate => _provider.GetRequiredService<DeflateCompressor>(),
                _ => throw new NotSupportedException($"Unsupported compression type: {type}")
            };

        /// <summary>
        /// Gets a compressor configured with the specified named profile.
        /// </summary>
        public ICompressor GetCompressor(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName) || profileName.Equals("default", StringComparison.OrdinalIgnoreCase))
                return GetCompressor();

            return _namedCache.GetOrAdd(profileName, name =>
            {
                var profileOptions = _optionsMonitor?.Get(name);
                if (profileOptions == null)
                {
                    throw new InvalidOperationException($"Compression profile '{name}' was not found.");
                }

                return CreateCompressor(profileOptions);
            });
        }

        /// <summary>
        /// Creates a dedicated compressor instance matching the specified options.
        /// </summary>
        public static ICompressor CreateCompressor(CompressionOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            return options.Algorithm switch
            {
                CompressionType.None => new NoneCompressor(),
                CompressionType.Brotli => new BrotliCompressor(options.Level),
                CompressionType.LZ4 => new Lz4Compressor(options.LZ4Level),
                CompressionType.Gzip => new GzipCompressor(options.Level),
                CompressionType.Zstd => new ZstdCompressor(options.ZstdLevel),
                CompressionType.Deflate => new DeflateCompressor(options.Level),
                _ => throw new NotSupportedException($"Unsupported compression type: {options.Algorithm}")
            };
        }
    }
}