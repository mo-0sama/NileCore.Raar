using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NileCore.Raar.Abstractions.Compression;
using NileCore.Raar.Caching.HybridCache;
using NileCore.Raar.Core.Compression;
using NileCore.Raar.Core.Extensions;
using Xunit;
using Xunit.Abstractions;

namespace NileCore.Raar.Core.Tests
{
    public class BaselineBenchmarkRunner
    {
        private readonly ITestOutputHelper _output;

        public BaselineBenchmarkRunner(ITestOutputHelper output)
        {
            _output = output;
        }

        public class BenchmarkResult
        {
            public string Category { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Target { get; set; } = string.Empty;
            public string PayloadSize { get; set; } = string.Empty;
            public int OriginalSizeBytes { get; set; }
            public int CompressedSizeBytes { get; set; }
            public double CompressionRatioPercent { get; set; }
            public double MeanExecutionTimeMicroseconds { get; set; }
            public long AllocatedBytesPerOp { get; set; }
            public int Iterations { get; set; }
        }

        public class BaselineSnapshot
        {
            public string Version { get; set; } = "1.0.4";
            public string Timestamp { get; set; } = DateTime.UtcNow.ToString("o");
            public string HostEnvironment { get; set; } = $"{System.Environment.OSVersion} | .NET {System.Environment.Version}";
            public List<BenchmarkResult> Benchmarks { get; set; } = new();
        }

        [Fact]
        public async Task Measure_Baseline_Performance_And_Save()
        {
            var snapshot = new BaselineSnapshot();

            // 1. Compression Benchmarks
            var compressors = new (string Name, ICompressor Compressor)[]
            {
                ("None", new NoneCompressor()),
                ("Brotli", new BrotliCompressor()),
                ("Gzip", new GzipCompressor()),
                ("LZ4", new Lz4Compressor()),
                ("Zstd", new ZstdCompressor())
            };

            var payloads = new (string Label, byte[] Data)[]
            {
                ("Small_100B", GeneratePayload(100)),
                ("Medium_10KB", GeneratePayload(10_000)),
                ("Large_1MB", GeneratePayload(1_000_000))
            };

            foreach (var (compName, comp) in compressors)
            {
                foreach (var (sizeLabel, data) in payloads)
                {
                    int iters = sizeLabel.Contains("1MB") ? 25 : (sizeLabel.Contains("10KB") ? 200 : 1000);

                    // Warmup
                    for (int i = 0; i < 5; i++)
                    {
                        var wComp = comp.Compress(data);
                        _ = comp.Decompress(wComp);
                    }

                    // Benchmark Compress
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    long startAlloc = GC.GetAllocatedBytesForCurrentThread();
                    var sw = Stopwatch.StartNew();
                    byte[] compressed = null!;
                    for (int i = 0; i < iters; i++)
                    {
                        compressed = comp.Compress(data);
                    }
                    sw.Stop();
                    long endAlloc = GC.GetAllocatedBytesForCurrentThread();

                    double compressMicros = (sw.Elapsed.TotalMilliseconds * 1000.0) / iters;
                    long compressAlloc = (endAlloc - startAlloc) / iters;
                    double ratio = data.Length > 0 ? ((double)compressed.Length / data.Length) * 100.0 : 100.0;

                    snapshot.Benchmarks.Add(new BenchmarkResult
                    {
                        Category = "Compression",
                        Name = "Compress",
                        Target = compName,
                        PayloadSize = sizeLabel,
                        OriginalSizeBytes = data.Length,
                        CompressedSizeBytes = compressed.Length,
                        CompressionRatioPercent = Math.Round(ratio, 2),
                        MeanExecutionTimeMicroseconds = Math.Round(compressMicros, 3),
                        AllocatedBytesPerOp = compressAlloc,
                        Iterations = iters
                    });

                    // Benchmark Decompress
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();

                    startAlloc = GC.GetAllocatedBytesForCurrentThread();
                    sw.Restart();
                    for (int i = 0; i < iters; i++)
                    {
                        _ = comp.Decompress(compressed);
                    }
                    sw.Stop();
                    endAlloc = GC.GetAllocatedBytesForCurrentThread();

                    double decompressMicros = (sw.Elapsed.TotalMilliseconds * 1000.0) / iters;
                    long decompressAlloc = (endAlloc - startAlloc) / iters;

                    snapshot.Benchmarks.Add(new BenchmarkResult
                    {
                        Category = "Compression",
                        Name = "Decompress",
                        Target = compName,
                        PayloadSize = sizeLabel,
                        OriginalSizeBytes = compressed.Length,
                        CompressedSizeBytes = data.Length,
                        CompressionRatioPercent = Math.Round(ratio, 2),
                        MeanExecutionTimeMicroseconds = Math.Round(decompressMicros, 3),
                        AllocatedBytesPerOp = decompressAlloc,
                        Iterations = iters
                    });
                }
            }

            // 2. StringExtensions Benchmarks
            int strIters = 5000;
            string testStr = "The quick brown fox jumps over the lazy dog 1234567890 NileCore Raar High Performance";

            // HashSHA256
            for (int i = 0; i < 50; i++) _ = testStr.HashSHA256();
            long sAlloc = GC.GetAllocatedBytesForCurrentThread();
            var timer = Stopwatch.StartNew();
            for (int i = 0; i < strIters; i++) _ = testStr.HashSHA256();
            timer.Stop();
            long eAlloc = GC.GetAllocatedBytesForCurrentThread();

            snapshot.Benchmarks.Add(new BenchmarkResult
            {
                Category = "StringExtensions",
                Name = "HashSHA256",
                Target = "StringExtensions",
                PayloadSize = $"{testStr.Length} chars",
                MeanExecutionTimeMicroseconds = Math.Round((timer.Elapsed.TotalMilliseconds * 1000.0) / strIters, 3),
                AllocatedBytesPerOp = (eAlloc - sAlloc) / strIters,
                Iterations = strIters
            });

            // Mask
            for (int i = 0; i < 50; i++) _ = testStr.Mask(4, 15);
            sAlloc = GC.GetAllocatedBytesForCurrentThread();
            timer.Restart();
            for (int i = 0; i < strIters; i++) _ = testStr.Mask(4, 15);
            timer.Stop();
            eAlloc = GC.GetAllocatedBytesForCurrentThread();

            snapshot.Benchmarks.Add(new BenchmarkResult
            {
                Category = "StringExtensions",
                Name = "Mask",
                Target = "StringExtensions",
                PayloadSize = $"{testStr.Length} chars",
                MeanExecutionTimeMicroseconds = Math.Round((timer.Elapsed.TotalMilliseconds * 1000.0) / strIters, 3),
                AllocatedBytesPerOp = (eAlloc - sAlloc) / strIters,
                Iterations = strIters
            });

            // AddLeadingZero
            for (int i = 0; i < 50; i++) _ = "42".AddLeadingZero(10);
            sAlloc = GC.GetAllocatedBytesForCurrentThread();
            timer.Restart();
            for (int i = 0; i < strIters; i++) _ = "42".AddLeadingZero(10);
            timer.Stop();
            eAlloc = GC.GetAllocatedBytesForCurrentThread();

            snapshot.Benchmarks.Add(new BenchmarkResult
            {
                Category = "StringExtensions",
                Name = "AddLeadingZero",
                Target = "StringExtensions",
                PayloadSize = "2 -> 10 chars",
                MeanExecutionTimeMicroseconds = Math.Round((timer.Elapsed.TotalMilliseconds * 1000.0) / strIters, 3),
                AllocatedBytesPerOp = (eAlloc - sAlloc) / strIters,
                Iterations = strIters
            });

            // 3. ObjectExtensions Benchmarks
            int objIters = 5000;
            var samplePoco = new SampleUser { Id = 101, Name = "Alice", Email = "alice@nilecore.org", IsActive = true };

            // ToDictionary
            for (int i = 0; i < 50; i++) _ = samplePoco.ToDictionary();
            sAlloc = GC.GetAllocatedBytesForCurrentThread();
            timer.Restart();
            for (int i = 0; i < objIters; i++) _ = samplePoco.ToDictionary();
            timer.Stop();
            eAlloc = GC.GetAllocatedBytesForCurrentThread();

            snapshot.Benchmarks.Add(new BenchmarkResult
            {
                Category = "ObjectExtensions",
                Name = "ToDictionary",
                Target = "ObjectExtenstions",
                PayloadSize = "4 properties",
                MeanExecutionTimeMicroseconds = Math.Round((timer.Elapsed.TotalMilliseconds * 1000.0) / objIters, 3),
                AllocatedBytesPerOp = (eAlloc - sAlloc) / objIters,
                Iterations = objIters
            });

            // IsEmpty on List
            var list = new List<string> { "item1", "item2" };
            for (int i = 0; i < 50; i++) _ = list.IsEmpty();
            sAlloc = GC.GetAllocatedBytesForCurrentThread();
            timer.Restart();
            for (int i = 0; i < objIters; i++) _ = list.IsEmpty();
            timer.Stop();
            eAlloc = GC.GetAllocatedBytesForCurrentThread();

            snapshot.Benchmarks.Add(new BenchmarkResult
            {
                Category = "ObjectExtensions",
                Name = "IsEmpty_List",
                Target = "ObjectExtenstions",
                PayloadSize = "2 items",
                MeanExecutionTimeMicroseconds = Math.Round((timer.Elapsed.TotalMilliseconds * 1000.0) / objIters, 3),
                AllocatedBytesPerOp = (eAlloc - sAlloc) / objIters,
                Iterations = objIters
            });

            // 4. HybridCacheService Benchmarks (MemoryCache L1)
            var memCache = new MemoryCache(new MemoryCacheOptions());
            var fakeDist = new FakeInMemoryDistributedCache();
            var options = Options.Create(new HybridCacheOptions { EnableMemoryCache = true, EnableDistributedCache = true });
            var services = new ServiceCollection();
            services.AddSingleton<ICompressor, NoneCompressor>();
            var sp = services.BuildServiceProvider();
            var cache = new HybridCacheService(memCache, fakeDist, options, sp, NullLogger<HybridCacheService>.Instance);

            int cacheIters = 2000;
            // SetAsync
            sAlloc = GC.GetAllocatedBytesForCurrentThread();
            timer.Restart();
            for (int i = 0; i < cacheIters; i++)
            {
                await cache.SetAsync($"bench-key-{i % 50}", samplePoco);
            }
            timer.Stop();
            eAlloc = GC.GetAllocatedBytesForCurrentThread();

            snapshot.Benchmarks.Add(new BenchmarkResult
            {
                Category = "HybridCache",
                Name = "SetAsync_L1_L2",
                Target = "HybridCacheService",
                PayloadSize = "SampleUser POCO",
                MeanExecutionTimeMicroseconds = Math.Round((timer.Elapsed.TotalMilliseconds * 1000.0) / cacheIters, 3),
                AllocatedBytesPerOp = (eAlloc - sAlloc) / cacheIters,
                Iterations = cacheIters
            });

            // GetAsync (Hit)
            sAlloc = GC.GetAllocatedBytesForCurrentThread();
            timer.Restart();
            for (int i = 0; i < cacheIters; i++)
            {
                _ = await cache.GetAsync<SampleUser>($"bench-key-{i % 50}");
            }
            timer.Stop();
            eAlloc = GC.GetAllocatedBytesForCurrentThread();

            snapshot.Benchmarks.Add(new BenchmarkResult
            {
                Category = "HybridCache",
                Name = "GetAsync_L1_Hit",
                Target = "HybridCacheService",
                PayloadSize = "SampleUser POCO",
                MeanExecutionTimeMicroseconds = Math.Round((timer.Elapsed.TotalMilliseconds * 1000.0) / cacheIters, 3),
                AllocatedBytesPerOp = (eAlloc - sAlloc) / cacheIters,
                Iterations = cacheIters
            });

            // GetOrSetAsync (Hit)
            sAlloc = GC.GetAllocatedBytesForCurrentThread();
            timer.Restart();
            for (int i = 0; i < cacheIters; i++)
            {
                _ = await cache.GetOrSetAsync($"bench-key-{i % 50}", () => Task.FromResult(new SampleUser()));
            }
            timer.Stop();
            eAlloc = GC.GetAllocatedBytesForCurrentThread();

            snapshot.Benchmarks.Add(new BenchmarkResult
            {
                Category = "HybridCache",
                Name = "GetOrSetAsync_L1_Hit",
                Target = "HybridCacheService",
                PayloadSize = "SampleUser POCO",
                MeanExecutionTimeMicroseconds = Math.Round((timer.Elapsed.TotalMilliseconds * 1000.0) / cacheIters, 3),
                AllocatedBytesPerOp = (eAlloc - sAlloc) / cacheIters,
                Iterations = cacheIters
            });

            // Output report
            var jsonOptions = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(snapshot, jsonOptions);

            string repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".."));
            string baselineDir = Path.Combine(repoRoot, "benchmarks", "baselines");
            string historyDir = Path.Combine(baselineDir, "history");

            Directory.CreateDirectory(baselineDir);
            Directory.CreateDirectory(historyDir);

            string latestJsonPath = Path.Combine(baselineDir, "latest.json");
            string historyJsonPath = Path.Combine(historyDir, $"v1.0.4_baseline.json");
            File.WriteAllText(latestJsonPath, json);
            File.WriteAllText(historyJsonPath, json);

            // Generate Markdown report
            var sb = new StringBuilder();
            sb.AppendLine("# Baseline Performance Report (v1.0.4 Pre-Change)");
            sb.AppendLine();
            sb.AppendLine($"- **Timestamp:** {snapshot.Timestamp}");
            sb.AppendLine($"- **Environment:** {snapshot.HostEnvironment}");
            sb.AppendLine($"- **Status:** Baseline recorded successfully before code improvements");
            sb.AppendLine();
            sb.AppendLine("## 1. Compression Performance");
            sb.AppendLine();
            sb.AppendLine("| Algorithm | Operation | Payload | Input Size | Output Size | Ratio | Mean Time (μs) | Allocated/Op |");
            sb.AppendLine("|-----------|-----------|---------|------------|-------------|-------|----------------|--------------|");

            foreach (var b in snapshot.Benchmarks.Where(b => b.Category == "Compression"))
            {
                sb.AppendLine($"| {b.Target} | {b.Name} | {b.PayloadSize} | {b.OriginalSizeBytes:N0} B | {b.CompressedSizeBytes:N0} B | {b.CompressionRatioPercent}% | {b.MeanExecutionTimeMicroseconds:N3} μs | {b.AllocatedBytesPerOp:N0} B |");
            }

            sb.AppendLine();
            sb.AppendLine("## 2. Core & Utilities Performance");
            sb.AppendLine();
            sb.AppendLine("| Category | Method | Target | Payload | Mean Time (μs) | Allocated/Op |");
            sb.AppendLine("|----------|--------|--------|---------|----------------|--------------|");

            foreach (var b in snapshot.Benchmarks.Where(b => b.Category == "StringExtensions" || b.Category == "ObjectExtensions"))
            {
                sb.AppendLine($"| {b.Category} | {b.Name} | {b.Target} | {b.PayloadSize} | {b.MeanExecutionTimeMicroseconds:N3} μs | {b.AllocatedBytesPerOp:N0} B |");
            }

            sb.AppendLine();
            sb.AppendLine("## 3. HybridCache Performance");
            sb.AppendLine();
            sb.AppendLine("| Operation | Payload | Mean Time (μs) | Allocated/Op | Iterations |");
            sb.AppendLine("|-----------|---------|----------------|--------------|------------|");

            foreach (var b in snapshot.Benchmarks.Where(b => b.Category == "HybridCache"))
            {
                sb.AppendLine($"| {b.Name} | {b.PayloadSize} | {b.MeanExecutionTimeMicroseconds:N3} μs | {b.AllocatedBytesPerOp:N0} B | {b.Iterations} |");
            }

            string reportMdPath = Path.Combine(baselineDir, "baseline_report.md");
            File.WriteAllText(reportMdPath, sb.ToString());

            _output.WriteLine("Saved baseline to:");
            _output.WriteLine(latestJsonPath);
            _output.WriteLine(reportMdPath);
        }

        private static byte[] GeneratePayload(int size)
        {
            var sb = new StringBuilder();
            string template = "{ \"id\": 12345, \"name\": \"NileCore Raar Benchmark Item\", \"timestamp\": \"2026-10-25T12:00:00Z\", \"tags\": [\"compression\", \"caching\", \"performance\"], \"description\": \"Lorem ipsum dolor sit amet, consectetur adipiscing elit. Sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.\" }\n";
            while (sb.Length < size)
            {
                sb.Append(template);
            }
            var raw = Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[size];
            Array.Copy(raw, result, Math.Min(raw.Length, size));
            return result;
        }

        public class SampleUser
        {
            public int Id { get; set; } = 1;
            public string Name { get; set; } = "Default";
            public string Email { get; set; } = "default@example.com";
            public bool IsActive { get; set; } = true;
        }

        public class FakeInMemoryDistributedCache : IDistributedCache
        {
            private readonly Dictionary<string, byte[]> _storage = new();

            public byte[]? Get(string key) => _storage.TryGetValue(key, out var val) ? val : null;
            public Task<byte[]?> GetAsync(string key, System.Threading.CancellationToken token = default) =>
                Task.FromResult(Get(key));

            public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => _storage[key] = value;
            public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, System.Threading.CancellationToken token = default)
            {
                Set(key, value, options);
                return Task.CompletedTask;
            }

            public void Refresh(string key) { }
            public Task RefreshAsync(string key, System.Threading.CancellationToken token = default) => Task.CompletedTask;

            public void Remove(string key) => _storage.Remove(key);
            public Task RemoveAsync(string key, System.Threading.CancellationToken token = default)
            {
                Remove(key);
                return Task.CompletedTask;
            }
        }
    }
}
