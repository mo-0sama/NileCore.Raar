# Baseline Performance Report (v1.0.4 Pre-Change)

- **Timestamp:** 2026-10-25T10:56:24.3755840Z
- **Environment:** Microsoft Windows NT 10.0.26200.0 | .NET 10.0.13
- **Status:** Baseline recorded successfully before code improvements

## 1. Compression Performance

| Algorithm | Operation | Payload | Input Size | Output Size | Ratio | Mean Time (μs) | Allocated/Op |
|-----------|-----------|---------|------------|-------------|-------|----------------|--------------|
| None | Compress | Small_100B | 100 B | 100 B | 100% | 0.064 μs | 0 B |
| None | Decompress | Small_100B | 100 B | 100 B | 100% | 0.057 μs | 0 B |
| None | Compress | Medium_10KB | 10,000 B | 10,000 B | 100% | 0.052 μs | 0 B |
| None | Decompress | Medium_10KB | 10,000 B | 10,000 B | 100% | 0.064 μs | 0 B |
| None | Compress | Large_1MB | 1,000,000 B | 1,000,000 B | 100% | 0.096 μs | 1 B |
| None | Decompress | Large_1MB | 1,000,000 B | 1,000,000 B | 100% | 0.104 μs | 0 B |
| Brotli | Compress | Small_100B | 100 B | 87 B | 87% | 62.250 μs | 641 B |
| Brotli | Decompress | Small_100B | 87 B | 100 B | 87% | 15.456 μs | 852 B |
| Brotli | Compress | Medium_10KB | 10,000 B | 220 B | 2.2% | 132.687 μs | 1,041 B |
| Brotli | Decompress | Medium_10KB | 220 B | 10,000 B | 2.2% | 76.403 μs | 21,280 B |
| Brotli | Compress | Large_1MB | 1,000,000 B | 221 B | 0.02% | 5,995.512 μs | 3,344 B |
| Brotli | Decompress | Large_1MB | 221 B | 1,000,000 B | 0.02% | 6,097.948 μs | 2,980,739 B |
| Gzip | Compress | Small_100B | 100 B | 113 B | 113% | 38.944 μs | 728 B |
| Gzip | Decompress | Small_100B | 113 B | 100 B | 113% | 12.405 μs | 971 B |
| Gzip | Compress | Medium_10KB | 10,000 B | 327 B | 3.27% | 60.308 μs | 1,505 B |
| Gzip | Decompress | Medium_10KB | 327 B | 10,000 B | 3.27% | 43.534 μs | 21,135 B |
| Gzip | Compress | Large_1MB | 1,000,000 B | 7,624 B | 0.76% | 1,640.536 μs | 16,202 B |
| Gzip | Decompress | Large_1MB | 7,624 B | 1,000,000 B | 0.76% | 2,542.364 μs | 2,984,213 B |
| LZ4 | Compress | Small_100B | 100 B | 110 B | 110% | 15.569 μs | 344 B |
| LZ4 | Decompress | Small_100B | 110 B | 100 B | 110% | 1.091 μs | 128 B |
| LZ4 | Compress | Medium_10KB | 10,000 B | 339 B | 3.39% | 92.306 μs | 10,512 B |
| LZ4 | Decompress | Medium_10KB | 339 B | 10,000 B | 3.39% | 42.920 μs | 10,024 B |
| LZ4 | Compress | Large_1MB | 1,000,000 B | 4,226 B | 0.42% | 1,492.604 μs | 1,008,582 B |
| LZ4 | Decompress | Large_1MB | 4,226 B | 1,000,000 B | 0.42% | 921.624 μs | 1,000,024 B |
| Zstd | Compress | Small_100B | 100 B | 111 B | 111% | 118.084 μs | 584 B |
| Zstd | Decompress | Small_100B | 111 B | 100 B | 111% | 50.705 μs | 440 B |
| Zstd | Compress | Medium_10KB | 10,000 B | 235 B | 2.35% | 295.471 μs | 10,776 B |
| Zstd | Decompress | Medium_10KB | 235 B | 10,000 B | 2.35% | 79.916 μs | 20,360 B |
| Zstd | Compress | Large_1MB | 1,000,000 B | 328 B | 0.03% | 5,997.740 μs | 1,004,761 B |
| Zstd | Decompress | Large_1MB | 328 B | 1,000,000 B | 0.03% | 2,116.876 μs | 2,002,320 B |

## 2. Core & Utilities Performance

| Category | Method | Target | Payload | Mean Time (μs) | Allocated/Op |
|----------|--------|--------|---------|----------------|--------------|
| StringExtensions | HashSHA256 | StringExtensions | 85 chars | 14.459 μs | 1,736 B |
| StringExtensions | Mask | StringExtensions | 85 chars | 0.426 μs | 440 B |
| StringExtensions | AddLeadingZero | StringExtensions | 2 -> 10 chars | 0.157 μs | 48 B |
| ObjectExtensions | ToDictionary | ObjectExtenstions | 4 properties | 3.695 μs | 520 B |
| ObjectExtensions | IsEmpty_List | ObjectExtenstions | 2 items | 0.118 μs | 0 B |

## 3. HybridCache Performance

| Operation | Payload | Mean Time (μs) | Allocated/Op | Iterations |
|-----------|---------|----------------|--------------|------------|
| SetAsync_L1_L2 | SampleUser POCO | 57.717 μs | 1,172 B | 2000 |
| GetAsync_L1_Hit | SampleUser POCO | 8.401 μs | 416 B | 2000 |
| GetOrSetAsync_L1_Hit | SampleUser POCO | 5.387 μs | 664 B | 2000 |
