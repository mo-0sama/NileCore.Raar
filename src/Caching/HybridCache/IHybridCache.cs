using System;
using System.Threading.Tasks;

namespace NileCore.Raar.Caching.HybridCache
{
    public interface IHybridCache
    {
        Task<T> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null) where T : class;

        Task<T> GetAsync<T>(string key) where T : class;

        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null) where T : class;

        Task RemoveAsync(string key);
    }
}