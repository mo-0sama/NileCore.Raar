using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;
using NileCore.Raar.Caching.HybridCache;
using NileCore.Raar.Core.Compression;
using Xunit;

namespace NileCore.Raar.Core.Tests
{
    public class HybridCacheServiceTests
    {
        public class TestUser
        {
            public int Id { get; set; } = 1;
            public string Name { get; set; } = "Test User";
            public string Email { get; set; } = "test@example.com";
        }

        private class InMemoryDistCache : IDistributedCache
        {
            private readonly Dictionary<string, byte[]> _dict = new();

            public byte[]? Get(string key) => _dict.TryGetValue(key, out var val) ? val : null;
            public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => Task.FromResult(Get(key));
            public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _dict[key] = value;
            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
            {
                Set(key, value, options);
                return Task.CompletedTask;
            }
            public void Refresh(string key) { }
            public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
            public void Remove(string key) => _dict.Remove(key);
            public Task RemoveAsync(string key, CancellationToken token = default)
            {
                Remove(key);
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task GetOrSetAsync_L1_Memory_Hit_Returns_Cached_Value()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            int factoryCalls = 0;
            Task<TestUser> Factory()
            {
                factoryCalls++;
                return Task.FromResult(new TestUser { Id = 42, Name = "Alice" });
            }

            var first = await cache.GetOrSetAsync("user-42", Factory);
            var second = await cache.GetOrSetAsync("user-42", Factory);

            Assert.Equal("Alice", first.Name);
            Assert.Equal("Alice", second.Name);
            Assert.Equal(1, factoryCalls); // Factory should only be called once!
        }

        [Fact]
        public async Task SetAsync_And_GetAsync_RoundTrip_With_Compression_And_Serialization()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var compressor = new BrotliCompressor();
            var serializer = new SystemTextJsonCacheSerializer();
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance, compressor, serializer);

            var user = new TestUser { Id = 99, Name = "Bob", Email = "bob@test.com" };
            await cache.SetAsync("user-99", user);

            var retrieved = await cache.GetAsync<TestUser>("user-99");
            Assert.NotNull(retrieved);
            Assert.Equal(99, retrieved!.Id);
            Assert.Equal("Bob", retrieved.Name);
        }

        [Fact]
        public async Task RemoveAsync_Clears_Both_L1_And_L2_Tiers()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            var user = new TestUser { Id = 100, Name = "Charlie" };
            await cache.SetAsync("user-100", user);

            var retrievedBefore = await cache.GetAsync<TestUser>("user-100");
            Assert.NotNull(retrievedBefore);

            await cache.RemoveAsync("user-100");

            var retrievedAfter = await cache.GetAsync<TestUser>("user-100");
            Assert.Null(retrievedAfter);
        }

        [Fact]
        public async Task KeyPrefix_Prepends_Prefix_To_Keys()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions
            {
                EnableMemoryCache = true,
                EnableDistributedCache = true,
                KeyPrefix = "tenant1"
            });
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            var user = new TestUser { Id = 7, Name = "PrefixTest" };
            await cache.SetAsync("user-7", user);

            // Check that the prefixed key exists in memory cache
            Assert.True(memCache.TryGetValue("tenant1:user-7", out _));

            var result = await cache.GetAsync<TestUser>("user-7");
            Assert.NotNull(result);
            Assert.Equal("PrefixTest", result!.Name);
        }

        [Fact]
        public async Task GetOrSetAsync_Cancellation_Honors_CancellationToken()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions());
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            {
                await cache.GetOrSetAsync("cancelled-key", async ct =>
                {
                    ct.ThrowIfCancellationRequested();
                    await Task.Delay(50, ct);
                    return new TestUser();
                }, cancellationToken: cts.Token);
            });
        }

        [Fact]
        public async Task SetAndGet_ValueTypes_Int_And_DateTime_And_CustomStruct_Succeeds()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            // 1. Primitive int
            await cache.SetAsync("int-key", 12345);
            var intResult = await cache.GetAsync<int>("int-key");
            Assert.Equal(12345, intResult);

            // 2. Value type DateTime
            var now = new DateTime(2026, 10, 25, 12, 0, 0, DateTimeKind.Utc);
            await cache.SetAsync("date-key", now);
            var dateResult = await cache.GetAsync<DateTime>("date-key");
            Assert.Equal(now, dateResult);

            // 3. Custom struct
            var point = new CustomPoint(15, 30);
            await cache.SetAsync("point-key", point);
            var pointResult = await cache.GetAsync<CustomPoint>("point-key");
            Assert.Equal(point, pointResult);
        }

        public readonly struct CustomPoint
        {
            public int X { get; }
            public int Y { get; }

            public CustomPoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        [Fact]
        public async Task InvalidateByTagAsync_Removes_All_Associated_Keys()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            await cache.SetAsync("prod-1", new TestUser { Id = 1, Name = "P1" }, tags: new[] { "catalog", "electronics" });
            await cache.SetAsync("prod-2", new TestUser { Id = 2, Name = "P2" }, tags: new[] { "catalog", "books" });
            await cache.SetAsync("prod-3", new TestUser { Id = 3, Name = "P3" }, tags: new[] { "furniture" });

            Assert.NotNull(await cache.GetAsync<TestUser>("prod-1"));
            Assert.NotNull(await cache.GetAsync<TestUser>("prod-2"));
            Assert.NotNull(await cache.GetAsync<TestUser>("prod-3"));

            // Invalidate "catalog" -> should remove prod-1 and prod-2, leave prod-3
            await cache.InvalidateByTagAsync("catalog");

            Assert.Null(await cache.GetAsync<TestUser>("prod-1"));
            Assert.Null(await cache.GetAsync<TestUser>("prod-2"));
            Assert.NotNull(await cache.GetAsync<TestUser>("prod-3"));
        }

        [Fact]
        public async Task InvalidateByTagsAsync_Removes_Across_Multiple_Tags()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            await cache.SetAsync("t-1", "val1", tags: new[] { "tagA" });
            await cache.SetAsync("t-2", "val2", tags: new[] { "tagB" });
            await cache.SetAsync("t-3", "val3", tags: new[] { "tagC" });

            await cache.InvalidateByTagsAsync(new[] { "tagA", "tagB" });

            Assert.Null(await cache.GetAsync<string>("t-1"));
            Assert.Null(await cache.GetAsync<string>("t-2"));
            Assert.Equal("val3", await cache.GetAsync<string>("t-3"));
        }

        [Fact]
        public async Task Bulk_Operations_GetMany_SetMany_RemoveMany_Work_Correctly()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            var items = new Dictionary<string, TestUser>
            {
                ["bulk-1"] = new TestUser { Id = 1, Name = "User 1" },
                ["bulk-2"] = new TestUser { Id = 2, Name = "User 2" },
                ["bulk-3"] = new TestUser { Id = 3, Name = "User 3" }
            };

            // SetManyAsync
            await cache.SetManyAsync(items);

            // GetManyAsync
            var retrieved = await cache.GetManyAsync<TestUser>(new[] { "bulk-1", "bulk-2", "bulk-3", "bulk-nonexistent" });
            Assert.Equal(4, retrieved.Count);
            Assert.Equal("User 1", retrieved["bulk-1"]?.Name);
            Assert.Equal("User 2", retrieved["bulk-2"]?.Name);
            Assert.Equal("User 3", retrieved["bulk-3"]?.Name);
            Assert.Null(retrieved["bulk-nonexistent"]);

            // RemoveManyAsync
            await cache.RemoveManyAsync(new[] { "bulk-1", "bulk-2" });
            var afterRemoval = await cache.GetManyAsync<TestUser>(new[] { "bulk-1", "bulk-2", "bulk-3" });
            Assert.Null(afterRemoval["bulk-1"]);
            Assert.Null(afterRemoval["bulk-2"]);
            Assert.NotNull(afterRemoval["bulk-3"]);
        }

        [Fact]
        public async Task SetAsync_With_SlidingExpiration_Caches_Successfully()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance);

            await cache.SetWithSlidingExpirationAsync("slide-key", "sliding-val", TimeSpan.FromMinutes(5));
            var retrieved = await cache.GetAsync<string>("slide-key");
            Assert.Equal("sliding-val", retrieved);
        }

        private class TestCacheEventHandler : ICacheEventHandler
        {
            public int Hits { get; private set; }
            public int Misses { get; private set; }
            public int Sets { get; private set; }
            public int Removes { get; private set; }
            public int Errors { get; private set; }

            public Task OnCacheHitAsync(string key, CacheLayer layer, CancellationToken cancellationToken = default)
            {
                Hits++;
                return Task.CompletedTask;
            }

            public Task OnCacheMissAsync(string key, CancellationToken cancellationToken = default)
            {
                Misses++;
                return Task.CompletedTask;
            }

            public Task OnCacheSetAsync(string key, CacheLayer layer, CancellationToken cancellationToken = default)
            {
                Sets++;
                return Task.CompletedTask;
            }

            public Task OnCacheRemovedAsync(string key, CancellationToken cancellationToken = default)
            {
                Removes++;
                return Task.CompletedTask;
            }

            public Task OnCacheErrorAsync(string key, Exception exception, CancellationToken cancellationToken = default)
            {
                Errors++;
                return Task.CompletedTask;
            }
        }

        [Fact]
        public async Task CacheEvents_Trigger_Hit_Miss_Set_Remove_Properly()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var handler = new TestCacheEventHandler();
            var cache = new HybridCacheService(memCache, distCache, options, NullLogger<HybridCacheService>.Instance, eventHandler: handler);

            // 1. Miss
            var miss = await cache.GetAsync<string>("missing-key");
            Assert.Null(miss);
            Assert.Equal(1, handler.Misses);

            // 2. Set
            await cache.SetAsync("event-key", "val");
            Assert.Equal(1, handler.Sets);

            // 3. Hit
            var hit = await cache.GetAsync<string>("event-key");
            Assert.Equal("val", hit);
            Assert.Equal(1, handler.Hits);

            // 4. Remove
            await cache.RemoveAsync("event-key");
            Assert.Equal(1, handler.Removes);
        }

        [Fact]
        public async Task HybridCacheHealthCheck_Returns_Healthy_Status()
        {
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var distCache = new InMemoryDistCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });

            var healthCheck = new HybridCacheHealthCheck(memCache, distCache, options);
            var result = await healthCheck.CheckHealthAsync(new Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckContext());

            Assert.Equal(Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Healthy, result.Status);
            Assert.Contains("operational", result.Description);
        }
    }
}
