# NileCore.Raar

A .NET library providing high-performance **compression** and **hybrid caching** utilities, designed for seamless integration via Microsoft Dependency Injection.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
![.NET Standard 2.1](https://img.shields.io/badge/.NET%20Standard-2.1-blue)
![Version](https://img.shields.io/badge/version-1.0.4-green)

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Compression Module](#compression-module)
- [Caching Module](#caching-module)
- [Core Utilities](#core-utilities)
- [Getting Started](#getting-started)
- [Dependencies](#dependencies)

---

## Architecture Overview

NileCore.Raar follows a **modular architecture** built around abstractions, with clear separation of concerns across multiple packages. The library leverages **Factory**, **Strategy**, and **Options** patterns from the Microsoft.Extensions ecosystem.

### High-Level Architecture Diagram

```
+------------------------------------------------------+
|                    Consumer Application               |
+------------------------------------------------------+
        |                              |
        v                              v
+------------------+       +------------------------+
|   Compression    |       |    HybridCache         |
|   Module         |       |    Module              |
|                  |       |                        |
|  services        |       |  services              |
|  .AddCompression |       |  .AddHybridCacheService|
+------------------+       +------------------------+
        |                       |            |
        v                       v            v
+------------------+   +-----------+  +-------------+
|  CompressorFactory|  | IMemory   |  | IDistributed|
|                  |   | Cache     |  | Cache       |
|  (Factory Pattern)|  | (L1)     |  | (L2/Redis)  |
+------------------+   +-----------+  +-------------+
        |
        v
+------------------------------------------------------+
|              ICompressor (Strategy Pattern)           |
|------------------------------------------------------|
| NoneCompressor | Brotli | Gzip |   LZ4   |   Zstd   |
+------------------------------------------------------+
        ^
        |
+------------------------------------------------------+
|            NileCore.Raar.Abstractions                 |
|           (Core Interfaces & Contracts)               |
+------------------------------------------------------+
```

### Request Flow - HybridCache with Compression

```
  Client Request
       |
       v
+------------------+
| GetOrSetAsync<T> |
+------------------+
       |
       v
  [Check L1 Memory Cache]
       |
   Found? ----YES----> [Validate Version against L2]
       |                        |
       NO               Version Match? --YES--> Return Value
       |                        |
       v                       NO
  [Check L2 Distributed Cache]  |
       |                        v
   Found? ----YES----> [Decompress] --> [Deserialize]
       |                    |
       NO                   v
       |              [Update L1 Cache]
       v                    |
  [Execute Factory]         v
       |              Return Value
       v
  [Serialize] --> [Compress]
       |
       v
  [Store in L1 + L2 with Version]
       |
       v
  Return Value
```

### Compression Algorithm Selection Flow

```
  Application Startup
       |
       v
  Configure CompressionOptions
  { Algorithm = CompressionType.XXX }
       |
       v
  services.AddCompression()
       |
       v
  CompressorFactory resolves ICompressor
       |
       +---> None  (CompressionType.None)   --> Pass-through
       +---> Brotli (CompressionType.Brotli) --> System.IO.Compression
       +---> Gzip   (CompressionType.Gzip)   --> System.IO.Compression
       +---> LZ4    (CompressionType.LZ4)    --> K4os.Compression.LZ4
       +---> Zstd   (CompressionType.Zstd)   --> ZstdSharp.Port
```

---

## Project Structure

```
NileCore.Raar/
|
+-- src/
|   |
|   +-- Core/
|   |   |
|   |   +-- Abstractions/              [NileCore.Raar.Abstractions]
|   |   |   +-- Compression/
|   |   |   |   +-- ICompressor.cs          Core compression interface
|   |   |   |   +-- NoneCompressor.cs       No-op compressor (default)
|   |   |   +-- Interfaces/
|   |   |   +-- Models/
|   |   |
|   |   +-- Compression/               [NileCore.Raar.Core.Compression]
|   |   |   +-- Algo/
|   |   |   |   +-- BrotliCompressor.cs     Brotli algorithm
|   |   |   |   +-- GzipCompressor.cs       Gzip algorithm
|   |   |   |   +-- LZ4Compressor.cs        LZ4 algorithm
|   |   |   |   +-- ZstdCompressor.cs       Zstandard algorithm
|   |   |   +-- CompressionOptions.cs       Configuration options
|   |   |   +-- CompressorFactory.cs        Factory for algorithm selection
|   |   |   +-- CompressionServiceCollectionExtensions.cs   DI registration
|   |   |
|   |   +-- Core/                      [NileCore.Raar.Core]
|   |   |   +-- Extensions/
|   |   |       +-- StringExtensions.cs     SHA256, Masking, Padding
|   |   |       +-- ObjectExtenstions.cs    ToDictionary, IsEmpty
|   |   |
|   |   +-- Json/                      [NileCore.Raar.Core.Json]
|   |   |   +-- (Compression for JSON serialization contexts)
|   |   |
|   |   +-- Net/                       [NileCore.Raar.Net]
|   |       +-- (Compression for networking contexts)
|   |
|   +-- Caching/
|       +-- HybridCache/               [NileCore.Raar.Caching.HybridCache]
|           +-- IHybridCache.cs             Cache interface
|           +-- HybridCacheOptions.cs       Cache configuration
|           +-- HybridCacheService.cs       Dual-layer cache implementation
|           +-- HybridCacheServiceCollectionExtensions.cs   DI registration
|
+-- tests/
|   +-- Core.Tests/                    [NileCore.Raar.Core.Tests]
|       +-- UnitTest1.cs
|
+-- build/
+-- Directory.Build.props              Shared build configuration
+-- NileCore.Raar.sln                  Solution file
```

### Package Dependency Graph

```
NileCore.Raar.Caching.HybridCache
  |
  +--------> NileCore.Raar.Abstractions  <--------+
  |                                                |
  +--------> NileCore.Raar.Core     NileCore.Raar.Core.Compression
                                                   |
                                    NileCore.Raar.Core.Json
                                                   |
                                    NileCore.Raar.Net
```

All library projects share `NileCore.Raar.Abstractions` as the foundational dependency providing `ICompressor` and `NoneCompressor`.

---

## Compression Module

### Supported Algorithms

| Algorithm | Type Enum | Library | Notes |
|-----------|-----------|---------|-------|
| **None** | `CompressionType.None` | Built-in | Pass-through, no compression |
| **Brotli** | `CompressionType.Brotli` | System.IO.Compression | .NET native, optimal level |
| **Gzip** | `CompressionType.Gzip` | System.IO.Compression | .NET native, optimal level |
| **LZ4** | `CompressionType.LZ4` | K4os.Compression.LZ4 | Fast compression with 8-byte header |
| **Zstd** | `CompressionType.Zstd` | ZstdSharp.Port | High ratio with 8-byte header |

### Design Patterns Used

| Pattern | Implementation | Purpose |
|---------|---------------|---------|
| **Strategy** | `ICompressor` + algorithm classes | Interchangeable compression algorithms |
| **Factory** | `CompressorFactory` | Runtime algorithm selection via configuration |
| **Options** | `CompressionOptions` | Type-safe configuration binding |
| **DI Registration** | `AddCompression()` extension | One-line service registration |

### Usage

```csharp
// In Startup / Program.cs
services.Configure<CompressionOptions>(options =>
{
    options.Algorithm = CompressionType.LZ4;
});
services.AddCompression();

// In your service
public class MyService
{
    private readonly ICompressor _compressor;

    public MyService(ICompressor compressor)
    {
        _compressor = compressor;
    }

    public byte[] CompressData(byte[] data) => _compressor.Compress(data);
    public byte[] DecompressData(byte[] data) => _compressor.Decompress(data);
}
```

---

## Caching Module

### HybridCache Architecture

The `HybridCacheService` implements a **dual-layer caching strategy** combining in-memory (L1) and distributed (L2) caches with automatic **version tracking** and **compression**.

```
+------------------------------------------+
|           HybridCacheService             |
|------------------------------------------|
|                                          |
|  +-- L1: IMemoryCache (30 min default)   |
|  |   Per-process, ultra-fast             |
|  |                                       |
|  +-- L2: IDistributedCache (4 hr default)|
|      Shared across processes (e.g. Redis)|
|      Compressed via ICompressor          |
|                                          |
|  +-- Version Tracking                   |
|      {key}:version in L2                 |
|      L1 invalidated on version mismatch  |
|                                          |
|  +-- Stampede Protection                 |
|      ConcurrentDictionary<Lazy<Task>>    |
|      Single factory execution per key    |
+------------------------------------------+
```

### Key Features

- **L1 + L2 caching** with independent TTLs
- **Version-based invalidation** - L1 is validated against L2 version on each read
- **Transparent compression** of distributed cache entries
- **Cache stampede protection** via `ConcurrentDictionary<string, Lazy<Task>>`
- **Configurable layers** - enable/disable memory or distributed cache independently

### Usage

```csharp
// In Startup / Program.cs
services.AddMemoryCache();
services.AddStackExchangeRedisCache(options => { /* Redis config */ });
services.Configure<HybridCacheOptions>(options =>
{
    options.EnableDistributedCache = true;
    options.EnableMemoryCache = true;
});
services.AddCompression();
services.AddHybridCacheService();

// In your service
public class ProductService
{
    private readonly IHybridCache _cache;

    public ProductService(IHybridCache cache)
    {
        _cache = cache;
    }

    public async Task<Product> GetProductAsync(string id)
    {
        return await _cache.GetOrSetAsync<Product>(
            key: $"product:{id}",
            factory: () => _dbContext.Products.FindAsync(id),
            memoryCacheExpiration: TimeSpan.FromMinutes(10),
            redisCacheExpiration: TimeSpan.FromHours(2)
        );
    }
}
```

---

## Core Utilities

### String Extensions

| Method | Description |
|--------|-------------|
| `HashSHA256()` | Computes SHA256 hash of a string |
| `AddLeadingZero(length)` | Pads string with leading zeros |
| `Mask(start, length)` | Masks portion of string with `*` |
| `Mask(start, length, char)` | Masks with custom character |

### Object Extensions

| Method | Description |
|--------|-------------|
| `ToDictionary()` | Converts object properties to `Dictionary<string, object>` via reflection |
| `IsEmpty<T>()` | Universal emptiness check (null, empty string, Guid.Empty, empty collections, default values) |

---

## Getting Started

### Requirements

- .NET Standard 2.1 compatible runtime (.NET Core 3.0+, .NET 5+)

### Installation

```bash
dotnet add package NileCore.Raar.Core.Compression
dotnet add package NileCore.Raar.Caching.HybridCache
dotnet add package NileCore.Raar.Core
```

### Minimal Setup

```csharp
var services = new ServiceCollection();

// Add compression with LZ4
services.Configure<CompressionOptions>(o => o.Algorithm = CompressionType.LZ4);
services.AddCompression();

// Add hybrid cache
services.AddMemoryCache();
services.Configure<HybridCacheOptions>(o => o.EnableDistributedCache = false); // Memory-only mode
services.AddHybridCacheService();
```

---

## Dependencies

| Package | Version | Used By |
|---------|---------|---------|
| K4os.Compression.LZ4 | 1.3.8 | Compression |
| ZstdSharp.Port | 0.8.6 | Compression |
| Newtonsoft.Json | 13.0.4 | HybridCache |
| Microsoft.Extensions.DependencyInjection | 8.0.0 | All modules |
| Microsoft.Extensions.Options | 8.0.0 | All modules |
| Microsoft.Extensions.Caching.Abstractions | 10.0.0 | HybridCache |
| Microsoft.Extensions.Logging | 8.0.0 | HybridCache |

---

## License

This project is licensed under the **MIT License**.
