# NileCore.Raar — Upgrade Guide (v1.0.x → v1.1.0)

This guide provides instructions and examples for upgrading projects utilizing the **NileCore.Raar** suite of NuGet packages from version `1.0.x` to `1.1.0`.

---

## Table of Contents
1. [Overview & Highlights](#1-overview--highlights)
2. [Breaking Changes & Deprecations](#2-breaking-changes--deprecations)
3. [Architecture & Packaging Changes](#3-architecture--packaging-changes)
4. [New APIs & Features](#4-new-apis--features)
5. [Migration Walkthroughs](#5-migration-walkthroughs)
6. [Performance Baseline & Verification](#6-performance-baseline--verification)

---

## 1. Overview & Highlights

Version 1.1.0 represents a significant performance, stability, and code quality upgrade:
- 🚀 **Zero Code Duplication**: Extracted shared compression engines into `NileCore.Raar.Core.Compression`.
- ⚡ **Full Async & Stream Support**: Async compression, stream streaming, and `CancellationToken` support end-to-end.
- 🛡️ **Pluggable Cache Serialization**: Native `System.Text.Json` support with fallback to `Newtonsoft.Json`.
- 🔒 **Fixed Stampede & Memory Leaks**: Scoped concurrency locks and distributed GUID versioning.
- 📦 **Rich Extensions**: Expanded String, Collection, DateTime, and Enum utility suites.
- 📊 **Performance Regression Gate**: Automated benchmark baselining.

---

## 2. Breaking Changes & Deprecations

### 2.1 Typo Fix: `ObjectExtenstions` → `ObjectExtensions`
- **Old Type:** `NileCore.Raar.Core.Extensions.ObjectExtenstions`
- **New Type:** `NileCore.Raar.Core.Extensions.ObjectExtensions`
- **Compatibility:** `ObjectExtenstions` is retained with `[Obsolete]` attribute so existing compiled code continues to function without immediate breakage.
- **Action Required:** Update source references to use `ObjectExtensions`.

```csharp
// Before (Legacy)
using NileCore.Raar.Core.Extensions;
bool empty = ObjectExtenstions.IsEmpty(myObj);

// After (Recommended)
using NileCore.Raar.Core.Extensions;
bool empty = myObj.IsEmpty(); // Or ObjectExtensions.IsEmpty(myObj);
```

### 2.2 `ICompressor` Interface Expansion
`ICompressor` now includes asynchronous and stream-based overloads:
```csharp
Task<byte[]> CompressAsync(byte[] data, CancellationToken cancellationToken = default);
Task<byte[]> DecompressAsync(byte[] data, CancellationToken cancellationToken = default);
Task CompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default);
Task DecompressAsync(Stream source, Stream destination, CancellationToken cancellationToken = default);
```
- Custom classes implementing `ICompressor` directly must implement the stream methods (`CompressAsync(Stream, Stream, ...)` and `DecompressAsync(Stream, Stream, ...)`). All built-in compressors (`BrotliCompressor`, `GzipCompressor`, `Lz4Compressor`, `ZstdCompressor`, `NoneCompressor`) fully implement these.

### 2.3 `IHybridCache` Cancellation Tokens
All async methods on `IHybridCache` now accept optional `CancellationToken cancellationToken = default`:
```csharp
Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, TimeSpan? memExp = null, TimeSpan? redisExp = null, CancellationToken ct = default);
Task<T> GetOrSetAsync<T>(string key, Func<CancellationToken, Task<T>> factory, TimeSpan? memExp = null, TimeSpan? redisExp = null, CancellationToken ct = default);
Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
Task SetAsync<T>(string key, T value, TimeSpan? memExp = null, TimeSpan? redisExp = null, CancellationToken ct = default);
Task RemoveAsync(string key, CancellationToken ct = default);
```
- Existing invocations without the parameter will continue to compile without source changes due to default parameter values.

---

## 3. Architecture & Packaging Changes

### 3.1 Unified Compression Dependencies
- `NileCore.Raar.Core.Json` and `NileCore.Raar.Net` no longer contain duplicated algorithm files.
- They now cleanly depend on `NileCore.Raar.Core.Compression`.
- You can freely combine `AddRaarCompression(...)` with `AddHybridCacheService(...)`.

### 3.2 Service Locator Removal in `HybridCacheService`
`HybridCacheService` previously looked up `ICompressor` via `serviceProvider.GetService<ICompressor>()` inside its constructor.
In v1.1.0, dependencies are injected directly:
```csharp
public HybridCacheService(
    IMemoryCache memoryCache,
    IDistributedCache distributedCache,
    IOptions<HybridCacheOptions> cacheSettings,
    ILogger<HybridCacheService> logger,
    ICompressor? compressor = null,
    ICacheSerializer? serializer = null)
```
A backward-compatible constructor taking `IServiceProvider` is retained for legacy compatibility.

---

## 4. New APIs & Features

### 4.1 Pluggable Serializer (`ICacheSerializer`)
Choose between high-speed `System.Text.Json` or `Newtonsoft.Json`:
```csharp
// Default is SystemTextJsonCacheSerializer
services.AddRaarHybridCache();

// Or configure custom options and serializer:
services.AddSingleton<ICacheSerializer, NewtonsoftJsonCacheSerializer>();
services.AddRaarHybridCache(options =>
{
    options.DefaultMemoryCacheExpiration = TimeSpan.FromMinutes(15);
    options.DefaultDistributedCacheExpiration = TimeSpan.FromHours(2);
    options.KeyPrefix = "app1";
});
```

### 4.2 Value Types, Structs & Primitives Support in Cache
The `where T : class` restriction on `IHybridCache` has been removed. You can now cache value types, primitives, and custom structs directly:
```csharp
await cache.SetAsync("max-retry-count", 5);
int retries = await cache.GetAsync<int>("max-retry-count");

await cache.SetAsync("server-uptime", DateTime.UtcNow);
DateTime uptime = await cache.GetAsync<DateTime>("server-uptime");
```

### 4.3 Tag-Based Invalidation, Sliding Expiration & Bulk Operations
Invalidate grouped entries, configure sliding TTLs, and execute bulk reads/writes:
```csharp
// Tag-based caching and invalidation
await cache.SetAsync("prod:1", product1, tags: new[] { "catalog", "electronics" });
await cache.SetAsync("prod:2", product2, tags: new[] { "catalog", "books" });
await cache.InvalidateByTagAsync("catalog"); // Purges all entries tagged "catalog"

// Sliding Expiration
await cache.SetWithSlidingExpirationAsync("user:session", session, TimeSpan.FromMinutes(30));

// Bulk Operations
var items = new Dictionary<string, Product> { ["p:1"] = p1, ["p:2"] = p2 };
await cache.SetManyAsync(items);
IDictionary<string, Product?> products = await cache.GetManyAsync<Product>(new[] { "p:1", "p:2", "p:3" });
await cache.RemoveManyAsync(new[] { "p:1", "p:2" });
```

### 4.4 Cache Lifecycle Events (`ICacheEventHandler`)
Subscribe to cache hits, misses, sets, removals, and errors for observability:
```csharp
public class MyCacheMetricsHandler : ICacheEventHandler
{
    public Task OnCacheHitAsync(string key, CacheLayer layer, CancellationToken ct = default) { /* track hit */ return Task.CompletedTask; }
    public Task OnCacheMissAsync(string key, CancellationToken ct = default) { /* track miss */ return Task.CompletedTask; }
    public Task OnCacheSetAsync(string key, CacheLayer layer, CancellationToken ct = default) { return Task.CompletedTask; }
    public Task OnCacheRemovedAsync(string key, CancellationToken ct = default) { return Task.CompletedTask; }
    public Task OnCacheErrorAsync(string key, Exception ex, CancellationToken ct = default) { return Task.CompletedTask; }
}

services.AddSingleton<ICacheEventHandler, MyCacheMetricsHandler>();
```

### 4.5 Deflate & Adaptive Compression (`NileCore.Raar.Core.Compression`)
- **Deflate**: Raw deflate algorithm via `DeflateCompressor` and `CompressionType.Deflate`.
- **Adaptive Compression**: Automatically bypasses compression when payloads are smaller than a configured threshold (default 256 bytes) where compression overhead exceeds savings:
```csharp
var compressor = new AdaptiveCompressor(new BrotliCompressor(), thresholdBytes: 256);
byte[] compressed = compressor.Compress(data); // Bypasses compression if data.Length < 256 bytes
```

### 4.6 Stream & JSON Extensions (`NileCore.Raar.Core.Json`)
Directly compress, decompress, serialize, and deep clone JSON objects:
```csharp
using NileCore.Raar.Core.Json;

// Serialization & Cloning
string json = myPoco.ToJson();
MyPoco? copy = json.FromJson<MyPoco>();
MyPoco? cloned = myPoco.DeepClone();

// Synchronous JSON Compression
byte[] compressedJson = compressor.CompressJson(myPoco);
MyPoco? restored = compressor.DecompressJson<MyPoco>(compressedJson);

// Asynchronous Stream JSON Compression
await compressor.CompressJsonAsync(myPoco, destinationStream, cancellationToken: ct);
MyPoco? fromStream = await compressor.DecompressJsonAsync<MyPoco>(sourceStream, cancellationToken: ct);
```

### 4.7 Network HTTP Content (`NileCore.Raar.Net`)
Transparently compress HTTP request bodies:
```csharp
using NileCore.Raar.Net;

var content = new StringContent(jsonString, Encoding.UTF8, "application/json");
var compressedContent = new CompressedHttpContent(content, compressor, "gzip");

await httpClient.PostAsync("/api/data", compressedContent);
```

### 4.8 New Utility Extensions (`NileCore.Raar.Core.Extensions`)

#### StringExtensions
- `HashSHA256()`, `HashSHA512()`, `HashMD5()`
- `Truncate(maxLength, suffix)`
- `ToSlug()` (URL slug generation)
- `ToCamelCase()`, `ToSnakeCase()`, `ToKebabCase()`
- `ToBase64()`, `FromBase64()`
- `ContainsAny(...)`, `SubstringSafe(...)`
- `IsValidEmail()`, `IsValidUrl()`

#### CollectionExtensions
- `ForEach(action)`
- `Batch(batchSize)`
- `DistinctBy(keySelector)`
- `IsNullOrEmpty()`
- `Shuffle()`

#### DateTimeExtensions
- `ToUnixTimestamp()`, `FromUnixTimestamp(long)`
- `StartOfDay()`, `EndOfDay()`
- `IsWeekend()`
- `ToIso8601()`

#### EnumExtensions
- `GetDescription()` (reads `[Description]` attribute)
- `ToList<TEnum>()`
- `Parse<TEnum>(string, ignoreCase, defaultValue)`

---

## 5. Migration Walkthroughs

### Upgrading Dependency Injection Setup

#### Prior Setup (v1.0.x):
```csharp
// Program.cs
services.AddCompression();
services.AddHybridCacheService();
```

#### Recommended Setup (v1.1.0):
```csharp
// Program.cs
using NileCore.Raar.Core.Compression;
using NileCore.Raar.Caching.HybridCache;

// Configure compression algorithm and levels
services.AddRaarCompression(options =>
{
    options.Algorithm = CompressionType.Brotli;
    options.Level = System.IO.Compression.CompressionLevel.Optimal;
});

// Configure hybrid cache with prefix and custom TTLs
services.AddHybridCacheService(options =>
{
    options.KeyPrefix = "ecommerce";
    options.DefaultMemoryCacheExpiration = TimeSpan.FromMinutes(20);
    options.DefaultDistributedCacheExpiration = TimeSpan.FromHours(6);
});
```

---

## 6. Performance Baseline & Verification

All changes in v1.1.0 have been verified against pre-change baselines using the regression comparison suite:
```powershell
# Run performance comparison test against recorded baseline
pwsh scripts/compare-baseline.ps1 -ThresholdPercent 20
```
Status: **✅ No performance regressions detected across all algorithms, hybrid caching operations, and core string/object utility suites.**
