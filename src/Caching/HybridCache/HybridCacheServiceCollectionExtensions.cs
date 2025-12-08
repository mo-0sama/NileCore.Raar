using Calc.Application.Caching;
using Microsoft.Extensions.DependencyInjection;
using NileCore.Raar.Core.HybridCache;
namespace NileCore.Raar.Core.HybridCache
{
    public static class HybridCacheServiceCollectionExtensions
    {
        public static IServiceCollection AddHybridCacheService(this IServiceCollection services)
        {
            //services.AddMemoryCache();
            services.AddSingleton<IHybridCache, HybridCacheService>();

            services.Configure<HybridCacheOptions>(_ => new HybridCacheOptions());
            //services.AddSingleton<HybridCacheOptions>(provider =>
            //{
            //    var options = provider.GetRequiredService<IOptions<HybridCacheOptions>>();
            //    return options.Value;
            //});

            return services;
        }
    }
}