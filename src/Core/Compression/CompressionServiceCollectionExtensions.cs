using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Extension methods for setting up compression services in an <see cref="IServiceCollection" />.
    /// </summary>
    public static class CompressionServiceCollectionExtensions
    {
        /// <summary>
        /// Registers compression services and compressors with default options.
        /// </summary>
        public static IServiceCollection AddCompression(this IServiceCollection services) =>
            services.AddCompression(_ => { });

        /// <summary>
        /// Registers compression services and compressors with default options (alias).
        /// </summary>
        public static IServiceCollection AddRaarCompression(this IServiceCollection services) =>
            services.AddCompression(_ => { });

        /// <summary>
        /// Registers compression services and compressors with configurable options (alias).
        /// </summary>
        public static IServiceCollection AddRaarCompression(this IServiceCollection services, Action<CompressionOptions> configure) =>
            services.AddCompression(configure);

        /// <summary>
        /// Registers a named compression profile with configurable options.
        /// </summary>
        public static IServiceCollection AddRaarCompression(
            this IServiceCollection services,
            string profileName,
            Action<CompressionOptions> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (string.IsNullOrWhiteSpace(profileName)) throw new ArgumentNullException(nameof(profileName));
            if (configure == null) throw new ArgumentNullException(nameof(configure));

            services.AddRaarCompression();
            services.Configure(profileName, configure);
            return services;
        }

        /// <summary>
        /// Registers a named compression profile with configurable options (alias).
        /// </summary>
        public static IServiceCollection AddCompression(
            this IServiceCollection services,
            string profileName,
            Action<CompressionOptions> configure) =>
            services.AddRaarCompression(profileName, configure);

        /// <summary>
        /// Registers compression services and compressors with configurable options.
        /// </summary>
        public static IServiceCollection AddCompression(this IServiceCollection services, Action<CompressionOptions> configure)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configure != null)
            {
                services.Configure(configure);
            }

            services.TryAddSingleton<NoneCompressor>();
            services.TryAddSingleton<BrotliCompressor>();
            services.TryAddSingleton<Lz4Compressor>();
            services.TryAddSingleton<GzipCompressor>();
            services.TryAddSingleton<ZstdCompressor>();
            services.TryAddSingleton<DeflateCompressor>();
            services.TryAddSingleton<CompressorFactory>();
            services.TryAddSingleton<ICompressorFactory>(provider => provider.GetRequiredService<CompressorFactory>());

            services.TryAddSingleton<ICompressor>(provider =>
            {
                var factory = provider.GetRequiredService<CompressorFactory>();
                return factory.GetCompressor();
            });

            return services;
        }
    }
}