using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NileCore.Raar.Caching.HybridCache
{
    /// <summary>
    /// Extension methods for registering <see cref="IHybridCache"/> and its dependencies in an <see cref="IServiceCollection"/>.
    /// </summary>
    public static class HybridCacheServiceCollectionExtensions
    {
        /// <summary>
        /// Registers <see cref="IHybridCache"/> using <see cref="HybridCacheService"/> with default options.
        /// </summary>
        public static IServiceCollection AddHybridCacheService(this IServiceCollection services)
        {
            return services.AddHybridCacheService(_ => { });
        }

        /// <summary>
        /// Registers <see cref="IHybridCache"/> with default options (alias).
        /// </summary>
        public static IServiceCollection AddRaarHybridCache(this IServiceCollection services) =>
            services.AddHybridCacheService(_ => { });

        /// <summary>
        /// Registers <see cref="IHybridCache"/> with custom options (alias).
        /// </summary>
        public static IServiceCollection AddRaarHybridCache(this IServiceCollection services, Action<HybridCacheOptions> configureOptions) =>
            services.AddHybridCacheService(configureOptions);

        /// <summary>
        /// Registers <see cref="IHybridCache"/> using <see cref="HybridCacheService"/> with custom options configuration.
        /// </summary>
        public static IServiceCollection AddHybridCacheService(
            this IServiceCollection services,
            Action<HybridCacheOptions> configureOptions)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (configureOptions == null) throw new ArgumentNullException(nameof(configureOptions));

            services.AddOptions();
            services.Configure(configureOptions);
            services.AddMemoryCache();

            services.TryAddSingleton<ICacheSerializer, SystemTextJsonCacheSerializer>();
            services.TryAddSingleton<IHybridCache, HybridCacheService>();

            return services;
        }
    }
}