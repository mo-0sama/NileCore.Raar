# Changelog

All notable changes to the NileCore.Raar project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2026-10-25

### Added
- **Asynchronous Compression**: `CompressAsync` and `DecompressAsync` in `ICompressor` and all implementations.
- **Stream Compression**: `CompressAsync(Stream, Stream)` and `DecompressAsync(Stream, Stream)` in `ICompressor`.
- **Cancellation Tokens**: Added `CancellationToken` support to `ICompressor` and `IHybridCache`.
- **Value Types & Struct Caching**: Removed `where T : class` constraint from `IHybridCache` to support caching primitives, value types, and custom structs.
- **Tag-Based Cache Invalidation**: `InvalidateByTagAsync` and `InvalidateByTagsAsync` for purging grouped entries.
- **Sliding Expiration**: `SetWithSlidingExpirationAsync` and sliding TTL support in both L1 memory and L2 distributed tiers.
- **Bulk Cache Operations**: `GetManyAsync`, `SetManyAsync`, and `RemoveManyAsync` for parallel batch operations.
- **Cache Lifecycle Events**: `ICacheEventHandler` interface and subscription support for cache hits, misses, writes, evictions, and errors.
- **Deflate Compression**: `DeflateCompressor` and `CompressionType.Deflate` using standard BCL `DeflateStream`.
- **Adaptive Compression**: `AdaptiveCompressor` to automatically bypass compression for small payloads (< 256 bytes) where compression overhead exceeds savings.
- **Pluggable Serializer**: `ICacheSerializer` interface with `SystemTextJsonCacheSerializer` (default) and `NewtonsoftJsonCacheSerializer`.
- **JSON Extensions**: `NileCore.Raar.Core.Json` extension methods for direct JSON compress/decompress with stream and async support, plus `ToJson`, `FromJson`, and `DeepClone`.
- **HTTP Content Wrapper**: `NileCore.Raar.Net.CompressedHttpContent` for transparent gzip/brotli/lz4 compression of HTTP request bodies.
- **Configurable Options**:
  - `CompressionOptions` with `Level`, `ZstdLevel`, `LZ4Level`.
  - `HybridCacheOptions` with `DefaultMemoryCacheExpiration`, `DefaultDistributedCacheExpiration`, `KeyPrefix`.
- **Core Utility Extensions**:
  - `StringExtensions`: `HashMD5`, `HashSHA512`, `Truncate`, `ToSlug`, `ToCamelCase`, `ToSnakeCase`, `ToKebabCase`, `ToBase64`, `FromBase64`, `ContainsAny`, `SubstringSafe`, `IsValidEmail`, `IsValidUrl`.
  - `ObjectExtensions`: `ThrowIfNull`.
  - `CollectionExtensions`: `ForEach`, `Batch`, `DistinctBy`, `IsNullOrEmpty`, `Shuffle`.
  - `DateTimeExtensions`: `ToUnixTimestamp`, `FromUnixTimestamp`, `StartOfDay`, `EndOfDay`, `IsWeekend`, `ToIso8601`.
  - `EnumExtensions`: `GetDescription`, `ToList`, `Parse`.
- **Test Suite**: 71 automated unit tests covering all compression algorithms, hybrid caching, serialization, and utilities.
- **Performance Baseline Suite**: Automated benchmark measurement and regression detection scripts (`scripts/save-baseline.ps1`, `scripts/compare-baseline.ps1`).
- **Upgrade Documentation**: Added `UPGRADE_GUIDE.md` for seamless migration from 1.0.x.

### Changed
- **Removed Code Duplication**: `NileCore.Raar.Core.Json` and `NileCore.Raar.Net` now reference `NileCore.Raar.Core.Compression` instead of maintaining duplicated algorithm files.
- **Constructor DI in HybridCache**: Removed service locator anti-pattern; `HybridCacheService` now uses constructor injection for `ICompressor` and `ICacheSerializer`.
- **Distributed Versioning**: Replaced `DateTime.Now.Ticks` with `Guid.NewGuid().ToString("N")` to eliminate clock skew and timestamp collision issues.
- **CI Pipeline**: Updated workflow to test on both .NET 8 and .NET 10, with automated PR baseline regression checks.
- **Nullable Reference Types**: Enabled nullable annotations across all projects.
- **Package Metadata**: Added SourceLink, deterministic build configuration, XML documentation file generation, and repository links to `Directory.Build.props`.

### Deprecated
- `ObjectExtenstions`: Fixed typo to `ObjectExtensions`. Deprecated old class with `[Obsolete]` for backward compatibility.

### Fixed
- **Cache Memory Leak**: Made `_factoryLocks` instance-scoped in `HybridCacheService` to prevent cross-instance memory retention.
- **Enumerator Disposal**: Fixed `ObjectExtensions.IsEmpty` to properly dispose `IEnumerator` instances in `finally` block.

---

## [1.0.4] - 2026-10-24
- Initial release with basic hybrid cache, compression algorithms, and string/object utilities.
