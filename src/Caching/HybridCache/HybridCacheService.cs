using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;
using NileCore.Raar.Core.Extensions;

namespace NileCore.Raar.Caching.HybridCache
{
    /// <summary>
    /// High-performance two-tier hybrid cache service with stampede protection, compression, pluggable serialization,
    /// sliding expiration, tag-based invalidation, and bulk operations.
    /// </summary>
    public class HybridCacheService : IHybridCache
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly HybridCacheOptions _cacheSettings;
        private readonly ILogger<HybridCacheService> _logger;
        private readonly ICompressor _compressor;
        private readonly ICacheSerializer _serializer;
        private readonly ICacheEventHandler? _eventHandler;

        // Instance-scoped locks prevent cross-instance memory leaks
        private readonly ConcurrentDictionary<string, Lazy<Task<object?>>> _factoryLocks = new();

        // In-memory index tracking keys associated with tags
        private readonly ConcurrentDictionary<string, ConcurrentBag<string>> _tagIndex = new();

        /// <summary>
        /// Initializes a new instance of <see cref="HybridCacheService"/> with direct dependency injection.
        /// </summary>
        [ActivatorUtilitiesConstructor]
        public HybridCacheService(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            IOptions<HybridCacheOptions> cacheSettings,
            ILogger<HybridCacheService> logger,
            ICompressor? compressor = null,
            ICacheSerializer? serializer = null,
            ICacheEventHandler? eventHandler = null)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _cacheSettings = cacheSettings?.Value ?? new HybridCacheOptions();
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _compressor = compressor ?? new NoneCompressor();
            _serializer = serializer ?? new SystemTextJsonCacheSerializer();
            _eventHandler = eventHandler;
        }

        /// <summary>
        /// Backward-compatible constructor accepting <see cref="IServiceProvider"/>.
        /// </summary>
        public HybridCacheService(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            IOptions<HybridCacheOptions> cacheSettings,
            IServiceProvider serviceProvider,
            ILogger<HybridCacheService> logger)
            : this(
                memoryCache,
                distributedCache,
                cacheSettings,
                logger,
                serviceProvider?.GetService<ICompressor>(),
                serviceProvider?.GetService<ICacheSerializer>(),
                serviceProvider?.GetService<ICacheEventHandler>())
        {
        }

        private string BuildKey(string key) =>
            string.IsNullOrEmpty(_cacheSettings.KeyPrefix) ? key : $"{_cacheSettings.KeyPrefix}:{key}";

        private string GetVersionKey(string key) => $"{BuildKey(key)}:version";

        private string GetTagKey(string tag) => $"{BuildKey(tag)}:tag";

        public Task<T> GetOrSetAsync<T>(
            string key,
            Func<Task<T>> factory,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            CancellationToken cancellationToken = default)
        {
            return GetOrSetAsync<T>(key, _ => factory(), memoryCacheExpiration, redisCacheExpiration, cancellationToken);
        }

        public async Task<T> GetOrSetAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> factory,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            CancellationToken cancellationToken = default)
        {
            var cached = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
            if (!EqualityComparer<T?>.Default.Equals(cached, default))
                return cached!;

            var lockKey = BuildKey(key);
            var lazy = _factoryLocks.GetOrAdd(lockKey, _ => new Lazy<Task<object?>>(async () =>
            {
                var secondCheck = await GetAsync<T>(key, cancellationToken).ConfigureAwait(false);
                if (!EqualityComparer<T?>.Default.Equals(secondCheck, default))
                    return secondCheck;

                var value = await factory(cancellationToken).ConfigureAwait(false);
                if (!EqualityComparer<T?>.Default.Equals(value, default))
                {
                    await SetAsync(key, value!, memoryCacheExpiration, redisCacheExpiration, cancellationToken).ConfigureAwait(false);
                }

                return value;
            }));

            try
            {
                var result = await lazy.Value.ConfigureAwait(false);
                return (T)result!;
            }
            finally
            {
                _factoryLocks.TryRemove(lockKey, out _);
            }
        }

        public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            string fullKey = BuildKey(key);
            try
            {
                // 1. Check L1 Memory Cache
                if (_cacheSettings.EnableMemoryCache && _memoryCache.TryGetValue(fullKey, out CachedItem<T>? cachedItem) && cachedItem != null)
                {
                    if (_cacheSettings.EnableDistributedCache)
                    {
                        var versionKey = GetVersionKey(key);
                        var redisVersion = await _distributedCache.GetStringAsync(versionKey, cancellationToken).ConfigureAwait(false);

                        if (!string.IsNullOrEmpty(redisVersion) && redisVersion == cachedItem.Version)
                        {
                            if (_eventHandler != null) await _eventHandler.OnCacheHitAsync(key, CacheLayer.Memory, cancellationToken).ConfigureAwait(false);
                            return cachedItem.Value;
                        }
                        else
                        {
                            _memoryCache.Remove(fullKey);
                        }
                    }
                    else
                    {
                        if (_eventHandler != null) await _eventHandler.OnCacheHitAsync(key, CacheLayer.Memory, cancellationToken).ConfigureAwait(false);
                        return cachedItem.Value;
                    }
                }

                // 2. Check L2 Distributed Cache
                if (_cacheSettings.EnableDistributedCache)
                {
                    var compressedBytes = await _distributedCache.GetAsync(fullKey, cancellationToken).ConfigureAwait(false);
                    if (compressedBytes != null && compressedBytes.Length > 0)
                    {
                        var decompressedBytes = await _compressor.DecompressAsync(compressedBytes, cancellationToken).ConfigureAwait(false);
                        var item = _serializer.Deserialize<CachedItem<T>>(decompressedBytes);

                        if (item != null)
                        {
                            if (_cacheSettings.EnableMemoryCache)
                            {
                                var defaultMemExp = _cacheSettings.DefaultMemoryCacheExpiration;
                                _memoryCache.Set(fullKey, item, new MemoryCacheEntryOptions
                                {
                                    AbsoluteExpirationRelativeToNow = defaultMemExp
                                });
                            }

                            if (_eventHandler != null) await _eventHandler.OnCacheHitAsync(key, CacheLayer.Distributed, cancellationToken).ConfigureAwait(false);
                            return item.Value;
                        }
                    }
                }

                if (_eventHandler != null) await _eventHandler.OnCacheMissAsync(key, cancellationToken).ConfigureAwait(false);
                return default;
            }
            catch (Exception ex)
            {
                if (_eventHandler != null) await _eventHandler.OnCacheErrorAsync(key, ex, cancellationToken).ConfigureAwait(false);
                _logger.LogError(ex, "Error getting cache for key: {Key}", key);
                return default;
            }
        }

        public Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            CancellationToken cancellationToken = default)
        {
            return SetAsync<T>(key, value, memoryCacheExpiration, redisCacheExpiration, null, null, cancellationToken);
        }

        public Task SetAsync<T>(
            string key,
            T value,
            IEnumerable<string> tags,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            TimeSpan? slidingExpiration = null,
            CancellationToken cancellationToken = default)
        {
            return SetAsync<T>(key, value, memoryCacheExpiration, redisCacheExpiration, slidingExpiration, tags, cancellationToken);
        }

        public Task SetWithSlidingExpirationAsync<T>(
            string key,
            T value,
            TimeSpan slidingExpiration,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            return SetAsync<T>(key, value, null, null, slidingExpiration, tags, cancellationToken);
        }

        public async Task SetAsync<T>(
            string key,
            T value,
            TimeSpan? memoryCacheExpiration,
            TimeSpan? redisCacheExpiration,
            TimeSpan? slidingExpiration,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            string fullKey = BuildKey(key);
            try
            {
                var memExp = memoryCacheExpiration ?? _cacheSettings.DefaultMemoryCacheExpiration;
                var distExp = redisCacheExpiration ?? _cacheSettings.DefaultDistributedCacheExpiration;
                var version = Guid.NewGuid().ToString("N");

                var cachedItem = new CachedItem<T>
                {
                    Value = value,
                    Version = version
                };

                // 1. Set L1 Memory Cache
                if (_cacheSettings.EnableMemoryCache)
                {
                    var memOptions = new MemoryCacheEntryOptions();
                    if (slidingExpiration.HasValue)
                    {
                        memOptions.SlidingExpiration = slidingExpiration.Value;
                    }
                    else
                    {
                        memOptions.AbsoluteExpirationRelativeToNow = memExp;
                    }

                    _memoryCache.Set(fullKey, cachedItem, memOptions);
                }

                // 2. Set L2 Distributed Cache
                if (_cacheSettings.EnableDistributedCache)
                {
                    var serializedBytes = _serializer.Serialize(cachedItem);
                    var compressedBytes = await _compressor.CompressAsync(serializedBytes, cancellationToken).ConfigureAwait(false);
                    var versionKey = GetVersionKey(key);

                    var distOptions = new DistributedCacheEntryOptions();
                    if (slidingExpiration.HasValue)
                    {
                        distOptions.SlidingExpiration = slidingExpiration.Value;
                    }
                    else
                    {
                        distOptions.AbsoluteExpirationRelativeToNow = distExp;
                    }

                    await _distributedCache.SetAsync(fullKey, compressedBytes, distOptions, cancellationToken).ConfigureAwait(false);
                    await _distributedCache.SetStringAsync(versionKey, version, distOptions, cancellationToken).ConfigureAwait(false);
                }

                // 3. Track Tags
                if (tags != null)
                {
                    foreach (var tag in tags)
                    {
                        if (string.IsNullOrWhiteSpace(tag)) continue;
                        string tagKey = GetTagKey(tag);
                        var bag = _tagIndex.GetOrAdd(tagKey, _ => new ConcurrentBag<string>());
                        bag.Add(key);
                    }
                }

                if (_eventHandler != null) await _eventHandler.OnCacheSetAsync(key, CacheLayer.Memory, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_eventHandler != null) await _eventHandler.OnCacheErrorAsync(key, ex, cancellationToken).ConfigureAwait(false);
                _logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            string fullKey = BuildKey(key);
            try
            {
                if (_cacheSettings.EnableMemoryCache)
                {
                    _memoryCache.Remove(fullKey);
                }

                if (_cacheSettings.EnableDistributedCache)
                {
                    var versionKey = GetVersionKey(key);
                    await _distributedCache.RemoveAsync(fullKey, cancellationToken).ConfigureAwait(false);
                    await _distributedCache.RemoveAsync(versionKey, cancellationToken).ConfigureAwait(false);
                }

                if (_eventHandler != null) await _eventHandler.OnCacheRemovedAsync(key, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (_eventHandler != null) await _eventHandler.OnCacheErrorAsync(key, ex, cancellationToken).ConfigureAwait(false);
                _logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }

        public async Task InvalidateByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(tag)) return;
            string tagKey = GetTagKey(tag);
            if (_tagIndex.TryRemove(tagKey, out var keys))
            {
                var distinctKeys = new HashSet<string>(keys);
                foreach (var k in distinctKeys)
                {
                    await RemoveAsync(k, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task InvalidateByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
        {
            if (tags == null) return;
            foreach (var tag in tags)
            {
                await InvalidateByTagAsync(tag, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<IDictionary<string, T?>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            if (keys == null) return new Dictionary<string, T?>();
            var keyList = keys.Distinct().ToList();
            var results = new ConcurrentDictionary<string, T?>();

            var tasks = keyList.Select(async k =>
            {
                var val = await GetAsync<T>(k, cancellationToken).ConfigureAwait(false);
                results[k] = val;
            });

            await Task.WhenAll(tasks).ConfigureAwait(false);
            return results;
        }

        public async Task SetManyAsync<T>(
            IDictionary<string, T> items,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null,
            CancellationToken cancellationToken = default)
        {
            if (items == null || items.Count == 0) return;

            var tasks = items.Select(kvp =>
                SetAsync(kvp.Key, kvp.Value, memoryCacheExpiration, redisCacheExpiration, cancellationToken));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
        {
            if (keys == null) return;
            var keyList = keys.Distinct().ToList();

            var tasks = keyList.Select(k => RemoveAsync(k, cancellationToken));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Wrapper for cached value and its version token for distributed cache synchronization.
    /// </summary>
    public class CachedItem<T>
    {
        public T Value { get; set; } = default!;
        public string Version { get; set; } = string.Empty;
    }
}