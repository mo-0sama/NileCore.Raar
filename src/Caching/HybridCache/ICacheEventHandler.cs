using System;
using System.Threading;
using System.Threading.Tasks;

namespace NileCore.Raar.Caching.HybridCache
{
    /// <summary>
    /// Represents the tier/layer of the cache where an event occurred.
    /// </summary>
    public enum CacheLayer
    {
        Memory,
        Distributed
    }

    /// <summary>
    /// Handler interface for listening to hybrid cache lifecycle events (hits, misses, writes, evictions, errors).
    /// </summary>
    public interface ICacheEventHandler
    {
        /// <summary>
        /// Invoked when a cache read is successfully satisfied from a cache layer.
        /// </summary>
        Task OnCacheHitAsync(string key, CacheLayer layer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked when a key is not found in either cache tier.
        /// </summary>
        Task OnCacheMissAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked when an item is successfully written to a cache layer.
        /// </summary>
        Task OnCacheSetAsync(string key, CacheLayer layer, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked when a cache entry is removed.
        /// </summary>
        Task OnCacheRemovedAsync(string key, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invoked when an exception occurs during cache operations.
        /// </summary>
        Task OnCacheErrorAsync(string key, Exception exception, CancellationToken cancellationToken = default);
    }
}
