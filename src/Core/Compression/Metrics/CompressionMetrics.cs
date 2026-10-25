using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// OpenTelemetry-compatible metrics instrumentation for compression operations.
    /// </summary>
    public static class CompressionMetrics
    {
        public const string MeterName = "NileCore.Raar.Compression";
        public const string MeterVersion = "1.2.0";

        private static readonly Meter Meter = new(MeterName, MeterVersion);

        /// <summary>
        /// Counter tracking total original (uncompressed) bytes.
        /// </summary>
        public static readonly Counter<long> OriginalBytesCounter = Meter.CreateCounter<long>(
            "raar.compression.original_bytes",
            unit: "By",
            description: "Total uncompressed payload bytes processed");

        /// <summary>
        /// Counter tracking total compressed bytes.
        /// </summary>
        public static readonly Counter<long> CompressedBytesCounter = Meter.CreateCounter<long>(
            "raar.compression.compressed_bytes",
            unit: "By",
            description: "Total compressed payload bytes produced");

        /// <summary>
        /// Histogram tracking duration of compression operations in milliseconds.
        /// </summary>
        public static readonly Histogram<double> DurationHistogram = Meter.CreateHistogram<double>(
            "raar.compression.duration_ms",
            unit: "ms",
            description: "Duration of compression and decompression operations in milliseconds");

        /// <summary>
        /// Records metrics for a completed compression operation.
        /// </summary>
        public static void RecordCompression(string algorithm, long originalBytes, long compressedBytes, double durationMs)
        {
            var tags = new TagList
            {
                { "algorithm", algorithm },
                { "operation", "compress" }
            };

            OriginalBytesCounter.Add(originalBytes, tags);
            CompressedBytesCounter.Add(compressedBytes, tags);
            DurationHistogram.Record(durationMs, tags);
        }

        /// <summary>
        /// Records metrics for a completed decompression operation.
        /// </summary>
        public static void RecordDecompression(string algorithm, long compressedBytes, long decompressedBytes, double durationMs)
        {
            var tags = new TagList
            {
                { "algorithm", algorithm },
                { "operation", "decompress" }
            };

            OriginalBytesCounter.Add(decompressedBytes, tags);
            CompressedBytesCounter.Add(compressedBytes, tags);
            DurationHistogram.Record(durationMs, tags);
        }
    }
}
