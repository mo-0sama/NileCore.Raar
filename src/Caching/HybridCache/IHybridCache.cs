using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NileCore.Raar.Caching.HybridCache
{
    /// <summary>
    /// Two-level hybrid cache abstraction combining in-memory L1 cache with distributed L2 cache.
    /// Supports both reference types and value types, tag-based invalidation, sliding expiration, and bulk operations.
    /// </summary>
    public interface IHybridCache
    {
        /// <summary>
        /// Gets an existing item or computes and caches it using the factory delegate.
        /// </summary>
        Task<T> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets an existing item or computes and caches it using the factory delegate with cancellation support.
        /// </summary>
        Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves an item from the cache.
        /// </summary>
        Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores an item into both memory and distributed cache with absolute expiration.
        /// </summary>
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores an item into both memory and distributed cache associated with tags and optional sliding expiration.
        /// </summary>
        Task SetAsync<T>(
            string key,
            T value,
            IEnumerable<string> tags,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores an item into both memory and distributed cache with optional sliding expiration and tag association.
        /// </summary>
        Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? memoryCacheExpiration,
            TimeSpan? redisCacheExpiration,
            TimeSpan? slidingExpiration,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores an item into cache with sliding expiration and optional tags.
        /// </summary>
        Task SetWithSlidingExpirationAsync<T>(
            string key,
            T value,
            TimeSpan slidingExpiration,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes an item from all cache tiers.
        /// </summary>
        Task RemoveAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates and removes all cache entries associated with the specified tag.
        /// </summary>
        Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invalidates and removes all cache entries associated with any of the specified tags.
        /// </summary>
        Task InvalidateByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves multiple items in parallel from the cache.
        /// </summary>
        Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default);

        /// <summary>
        /// Stores multiple items in parallel into the cache.
        /// </summary>
        Task SetManyAsync<T>(
            IDictionary<string, T> items,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes multiple items in parallel from the cache.
        /// </summary>
        Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default);
    }
}