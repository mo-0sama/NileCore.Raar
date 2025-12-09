using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Concurrent;
using System.Text;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NileCore.Raar.Abstractions.Compression;
using NileCore.Raar.Core.Extensions;

namespace NileCore.Raar.Caching.HybridCache
{
    public class HybridCacheService: IHybridCache
    {
        public HybridCacheService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        IOptions<HybridCacheOptions> cacheSettings,
        IServiceProvider serviceProvider,
        ILogger<HybridCacheService> logger)
        {
            this.memoryCache = memoryCache;
            this.logger = logger;
            this.distributedCache = distributedCache;
            _cacheSettings = cacheSettings.Value;
            this.compressor = serviceProvider.GetService<ICompressor>() ?? new NoneCompressor();
        }
        private readonly HybridCacheOptions _cacheSettings;
        private readonly ICompressor compressor;
        private readonly IMemoryCache memoryCache;
        private readonly IDistributedCache distributedCache;
        private readonly ILogger<HybridCacheService> logger;
        private static readonly ConcurrentDictionary<string, Lazy<Task<object>>> _factoryLocks = new ConcurrentDictionary<string, Lazy<Task<object>>>();
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new ProtectedPropertyContractResolver()
        };

        private static string GetVersionKey(string key) => $"{key}:version";

        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null) where T : class
        {
            var cached = await GetAsync<T>(key);
            if (cached != null)
                return cached;

            var lazy = _factoryLocks.GetOrAdd(key, _ => new Lazy<Task<object>>(async () =>
            {
                var secondCheck = await GetAsync<T>(key);
                if (secondCheck.IsEmpty())
                    return secondCheck;

                var value = await factory();
                if (!value.IsEmpty())
                    await SetAsync(key, value, memoryCacheExpiration, redisCacheExpiration);

                return value!;
            }));

            try
            {
                return (T)await lazy.Value;
            }
            finally
            {
                _factoryLocks.TryRemove(key, out _);
            }
        }

        public async Task<T> GetAsync<T>(string key) where T : class
        {
            try
            {
                // 1. Check Memory Cache (No change, still uses CachedItem<T>)
                if (_cacheSettings.EnableMemoryCache && memoryCache.TryGetValue(key, out CachedItem<T> cachedItem))
                {
                    if (_cacheSettings.EnableDistributedCache)
                    {
                        var versionKey = GetVersionKey(key);
                        var redisVersion = await distributedCache.GetStringAsync(versionKey); // Version is small, keep as string

                        if (!string.IsNullOrEmpty(redisVersion) && redisVersion == cachedItem.Version)
                        {
                            return cachedItem.Value;
                        }
                        else
                        {
                            memoryCache.Remove(key);
                        }
                    }
                    else
                    {
                        return cachedItem.Value;
                    }
                }

                if (_cacheSettings.EnableDistributedCache)
                {
                    var compressedBytes = await distributedCache.GetAsync(key);

                    if (compressedBytes != null && compressedBytes.Length > 0)
                    {
                        var decompressedBytes = compressor.Decompress(compressedBytes);

                        var redisValue = Encoding.UTF8.GetString(decompressedBytes);
                        var deserializedValue = JsonConvert.DeserializeObject<T>(redisValue, SerializerSettings);

                        var versionKey = GetVersionKey(key);
                        var redisVersion = await distributedCache.GetStringAsync(versionKey);

                        if (_cacheSettings.EnableMemoryCache)
                        {
                            var cacheItem = new CachedItem<T>
                            {
                                Value = deserializedValue,
                                Version = redisVersion ?? Guid.NewGuid().ToString() // Use version from Redis if available
                            };
                            memoryCache.Set(key, cacheItem, TimeSpan.FromMinutes(30));
                        }

                        return deserializedValue;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error getting cache for key: {Key}", key);
                return null;
            }
        }

        public async Task SetAsync<T>(string key, T value,
            TimeSpan? memoryCacheExpiration = null,
            TimeSpan? redisCacheExpiration = null) where T : class
        {
            try
            {
                var memExpiration = memoryCacheExpiration ?? TimeSpan.FromMinutes(30);
                var redisExpiration = redisCacheExpiration ?? TimeSpan.FromHours(4);

                var version = DateTime.Now.Ticks.ToString();

                if (_cacheSettings.EnableDistributedCache)
                {
                    var serializedString = JsonConvert.SerializeObject(value, SerializerSettings);
                    var serializedBytes = Encoding.UTF8.GetBytes(serializedString);

                    var compressedBytes = compressor.Compress(serializedBytes);

                    var versionKey = GetVersionKey(key);

                    await distributedCache.SetAsync(key, compressedBytes, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = redisExpiration
                    });

                    await distributedCache.SetStringAsync(versionKey, version, new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = redisExpiration
                    });
                }

                if (_cacheSettings.EnableMemoryCache)
                {
                    var cacheItem = new CachedItem<T>
                    {
                        Value = value,
                        Version = version
                    };
                    memoryCache.Set(key, cacheItem, memExpiration);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error setting cache for key: {Key}", key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                if (_cacheSettings.EnableMemoryCache)
                {
                    memoryCache.Remove(key);
                }

                if (_cacheSettings.EnableDistributedCache)
                {
                    var versionKey = GetVersionKey(key);

                    await distributedCache.RemoveAsync(key);
                    await distributedCache.RemoveAsync(versionKey);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error removing cache for key: {Key}", key);
            }
        }
    }

    internal class CachedItem<T>
    {
        public T Value { get; set; }
        public string Version { get; set; }
    }

    internal class ProtectedPropertyContractResolver : DefaultContractResolver
    {
        protected override Newtonsoft.Json.Serialization.JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization)
        {
            var property = base.CreateProperty(member, memberSerialization);

            if (member is System.Reflection.PropertyInfo propInfo)
            {
                if (!property.Writable)
                {
                    var setter = propInfo.GetSetMethod(nonPublic: true);
                    if (setter != null)
                    {
                        property.Writable = true;
                    }
                }
            }

            return property;
        }
    }
}