namespace NileCore.Raar.Caching.HybridCache
{
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
        public bool EnableDistributedCache { get; set; } = true;

        public bool EnableMemoryCache { get; set; } = true;
    }
}