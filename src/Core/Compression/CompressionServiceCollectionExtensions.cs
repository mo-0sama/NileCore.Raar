using Microsoft.Extensions.DependencyInjection;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    public static class CompressionServiceCollectionExtensions
    {
        public static IServiceCollection AddCompression(this IServiceCollection services)
        {
            services.AddSingleton<NoneCompressor>();
            services.AddSingleton<BrotliCompressor>();
            services.AddSingleton<Lz4Compressor>();
            services.AddSingleton<GzipCompressor>();
            services.AddSingleton<ZstdCompressor>();
            services.AddSingleton<CompressorFactory>();
            services.AddSingleton<ICompressor>(provider =>
            {
                var factory = provider.GetRequiredService<CompressorFactory>();
                return factory.GetCompressor();
            });

            return services;
        }
    }
}