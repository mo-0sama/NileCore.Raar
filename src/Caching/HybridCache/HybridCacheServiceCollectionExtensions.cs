using Microsoft.Extensions.DependencyInjection;
namespace NileCore.Raar.Caching.HybridCache
{
    public static class HybridCacheServiceCollectionExtensions
    {
        public static IServiceCollection AddHybridCacheService(this IServiceCollection services)
        {
            services.AddSingleton<IHybridCache, HybridCacheService>();
            return services;
        }
    }
}