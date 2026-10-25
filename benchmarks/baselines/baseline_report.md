# Baseline Performance Report (v1.0.4 Pre-Change)

- **Timestamp:** 2026-10-25T12:02:30.7878566Z
- **Environment:** Microsoft Windows NT 10.0.26200.0 | .NET 10.0.13
- **Status:** Baseline recorded successfully before code improvements

## 1. Compression Performance

| Algorithm | Operation | Payload | Input Size | Output Size | Ratio | Mean Time (μs) | Allocated/Op |
|-----------|-----------|---------|------------|-------------|-------|----------------|--------------|
| None | Compress | Small_100B | 100 B | 100 B | 100% | 0.050 μs | 0 B |
| None | Decompress | Small_100B | 100 B | 100 B | 100% | 0.053 μs | 0 B |
| None | Compress | Medium_10KB | 10,000 B | 10,000 B | 100% | 0.044 μs | 0 B |
| None | Decompress | Medium_10KB | 10,000 B | 10,000 B | 100% | 0.045 μs | 0 B |
| None | Compress | Large_1MB | 1,000,000 B | 1,000,000 B | 100% | 0.080 μs | 1 B |
| None | Decompress | Large_1MB | 1,000,000 B | 1,000,000 B | 100% | 0.108 μs | 0 B |
| Brotli | Compress | Small_100B | 100 B | 87 B | 87% | 68.083 μs | 641 B |
| Brotli | Decompress | Small_100B | 87 B | 100 B | 87% | 13.492 μs | 852 B |
| Brotli | Compress | Medium_10KB | 10,000 B | 220 B | 2.2% | 134.817 μs | 1,041 B |
| Brotli | Decompress | Medium_10KB | 220 B | 10,000 B | 2.2% | 65.324 μs | 21,280 B |
| Brotli | Compress | Large_1MB | 1,000,000 B | 221 B | 0.02% | 4,386.400 μs | 3,344 B |
| Brotli | Decompress | Large_1MB | 221 B | 1,000,000 B | 0.02% | 4,177.008 μs | 2,978,240 B |
| Gzip | Compress | Small_100B | 100 B | 113 B | 113% | 26.825 μs | 728 B |
| Gzip | Decompress | Small_100B | 113 B | 100 B | 113% | 8.793 μs | 971 B |
| Gzip | Compress | Medium_10KB | 10,000 B | 327 B | 3.27% | 27.744 μs | 1,505 B |
| Gzip | Decompress | Medium_10KB | 327 B | 10,000 B | 3.27% | 19.898 μs | 21,135 B |
| Gzip | Compress | Large_1MB | 1,000,000 B | 7,624 B | 0.76% | 651.492 μs | 16,202 B |
| Gzip | Decompress | Large_1MB | 7,624 B | 1,000,000 B | 0.76% | 1,320.028 μs | 2,973,940 B |
| LZ4 | Compress | Small_100B | 100 B | 110 B | 110% | 9.026 μs | 344 B |
| LZ4 | Decompress | Small_100B | 110 B | 100 B | 110% | 0.723 μs | 128 B |
| LZ4 | Compress | Medium_10KB | 10,000 B | 339 B | 3.39% | 48.730 μs | 10,512 B |
| LZ4 | Decompress | Medium_10KB | 339 B | 10,000 B | 3.39% | 24.170 μs | 10,024 B |
| LZ4 | Compress | Large_1MB | 1,000,000 B | 4,226 B | 0.42% | 880.728 μs | 1,008,757 B |
| LZ4 | Decompress | Large_1MB | 4,226 B | 1,000,000 B | 0.42% | 491.124 μs | 1,000,024 B |
| Zstd | Compress | Small_100B | 100 B | 111 B | 111% | 69.601 μs | 584 B |
| Zstd | Decompress | Small_100B | 111 B | 100 B | 111% | 25.609 μs | 440 B |
| Zstd | Compress | Medium_10KB | 10,000 B | 235 B | 2.35% | 144.228 μs | 10,776 B |
| Zstd | Decompress | Medium_10KB | 235 B | 10,000 B | 2.35% | 49.078 μs | 20,360 B |
| Zstd | Compress | Large_1MB | 1,000,000 B | 328 B | 0.03% | 2,937.160 μs | 1,004,761 B |
| Zstd | Decompress | Large_1MB | 328 B | 1,000,000 B | 0.03% | 1,262.232 μs | 2,001,976 B |

## 2. Core & Utilities Performance

| Category | Method | Target | Payload | Mean Time (μs) | Allocated/Op |
|----------|--------|--------|---------|----------------|--------------|
| StringExtensions | HashSHA256 | StringExtensions | 85 chars | 9.318 μs | 1,736 B |
| StringExtensions | Mask | StringExtensions | 85 chars | 0.348 μs | 440 B |
| StringExtensions | AddLeadingZero | StringExtensions | 2 -> 10 chars | 0.108 μs | 48 B |
| ObjectExtensions | ToDictionary | ObjectExtenstions | 4 properties | 1.428 μs | 520 B |
| ObjectExtensions | IsEmpty_List | ObjectExtenstions | 2 items | 0.172 μs | 0 B |

## 3. HybridCache Performance

| Operation | Payload | Mean Time (μs) | Allocated/Op | Iterations |
|-----------|---------|----------------|--------------|------------|
| SetAsync_L1_L2 | SampleUser POCO | 12.544 μs | 1,146 B | 2000 |
| GetAsync_L1_Hit | SampleUser POCO | 2.459 μs | 416 B | 2000 |
| GetOrSetAsync_L1_Hit | SampleUser POCO | 1.962 μs | 664 B | 2000 |
