using Microsoft.Extensions.DependencyInjection;
namespace NileCore.Raar.Caching.HybridCache
{
    public static class HybridCacheServiceCollectionExtensions
    {
        public static IServiceCollection AddHybridCacheService(this IServiceCollection services)
        {
            //services.AddMemoryCache();
            services.AddSingleton<IHybridCache, HybridCacheService>();

            services.AddOptions<HybridCacheOptions>();

            return services;
        }
    }
}