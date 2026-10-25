using System;

namespace NileCore.Raar.Caching.HybridCache
{
    /// <summary>
    /// Configuration options for the hybrid cache service.
    /// </summary>
    public class HybridCacheOptions
    {
        public HybridCacheOptions()
        {
        }

        public HybridCacheOptions(bool enableDistributedCache, bool enableMemoryCache)
        {
            EnableDistributedCache = enableDistributedCache;
            EnableMemoryCache = enableMemoryCache;
        }

        /// <summary>
        /// Enables or disables the distributed (L2) cache layer. Defaults to true.
        /// </summary>
        public bool EnableDistributedCache { get; set; } = true;

        /// <summary>
        /// Enables or disables the in-memory (L1) cache layer. Defaults to true.
        /// </summary>
        public bool EnableMemoryCache { get; set; } = true;

        /// <summary>
        /// Default expiration time for in-memory (L1) cache entries if not explicitly specified on call. Defaults to 30 minutes.
        /// </summary>
        public TimeSpan DefaultMemoryCacheExpiration { get; set; } = TimeSpan.FromMinutes(30);

        /// <summary>
        /// Default expiration time for distributed (L2) cache entries if not explicitly specified on call. Defaults to 4 hours.
        /// </summary>
        public TimeSpan DefaultDistributedCacheExpiration { get; set; } = TimeSpan.FromHours(4);

        /// <summary>
        /// Optional prefix prepended to all cache keys to avoid collisions across multiple services or tenants.
        /// </summary>
        public string KeyPrefix { get; set; } = string.Empty;
    }
}