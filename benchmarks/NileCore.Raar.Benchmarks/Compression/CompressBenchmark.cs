using System;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using NileCore.Raar.Abstractions.Compression;
using NileCore.Raar.Core.Compression;

namespace NileCore.Raar.Benchmarks.Compression
{
    [MemoryDiagnoser]
    [RankColumn]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [MarkdownExporterAttribute.GitHub]
    public class CompressBenchmark
    {
        private byte[] _smallPayload = null!;
        private byte[] _mediumPayload = null!;
        private byte[] _largePayload = null!;

        private readonly ICompressor _none = new NoneCompressor();
        private readonly ICompressor _brotli = new BrotliCompressor();
        private readonly ICompressor _gzip = new GzipCompressor();
        private readonly ICompressor _lz4 = new Lz4Compressor();
        private readonly ICompressor _zstd = new ZstdCompressor();

        [GlobalSetup]
        public void Setup()
        {
            _smallPayload = GeneratePayload(100);
            _mediumPayload = GeneratePayload(10_000);
            _largePayload = GeneratePayload(1_000_000);
        }

        private byte[] GetPayload(string size) => size switch
        {
            "Small" => _smallPayload,
            "Medium" => _mediumPayload,
            "Large" => _largePayload,
            _ => _smallPayload
        };

        [Benchmark(Baseline = true)]
        [Arguments("Small")]
        [Arguments("Medium")]
        [Arguments("Large")]
        public byte[] None(string size) => _none.Compress(GetPayload(size));

        [Benchmark]
        [Arguments("Small")]
        [Arguments("Medium")]
        [Arguments("Large")]
        public byte[] Brotli(string size) => _brotli.Compress(GetPayload(size));

        [Benchmark]
        [Arguments("Small")]
        [Arguments("Medium")]
        [Arguments("Large")]
        public byte[] LZ4(string size) => _lz4.Compress(GetPayload(size));

        [Benchmark]
        [Arguments("Small")]
        [Arguments("Medium")]
        [Arguments("Large")]
        public byte[] Gzip(string size) => _gzip.Compress(GetPayload(size));

        [Benchmark]
        [Arguments("Small")]
        [Arguments("Medium")]
        [Arguments("Large")]
        public byte[] Zstd(string size) => _zstd.Compress(GetPayload(size));

        private static byte[] GeneratePayload(int size)
        {
            var sb = new StringBuilder();
            string template = "{ \"id\": 12345, \"name\": \"NileCore Raar Benchmark Item\", \"timestamp\": \"2026-10-25T12:00:00Z\", \"tags\": [\"compression\", \"caching\", \"performance\"] }\n";
            while (sb.Length < size) sb.Append(template);
            var raw = Encoding.UTF8.GetBytes(sb.ToString());
            var res = new byte[size];
            Array.Copy(raw, res, Math.Min(raw.Length, size));
            return res;
        }
    }
}
