using System;
using NileCore.Raar.Abstractions.Compression;

namespace NileCore.Raar.Core.Compression
{
    /// <summary>
    /// Extension methods for attaching OpenTelemetry metrics to compressors.
    /// </summary>
    public static class CompressorMetricsExtensions
    {
        /// <summary>
        /// Wraps the compressor with an <see cref="InstrumentedCompressor"/> that records OpenTelemetry metrics.
        /// </summary>
        public static ICompressor WithMetrics(this ICompressor compressor, string? algorithmName = null)
        {
            if (compressor == null) throw new ArgumentNullException(nameof(compressor));
            if (compressor is InstrumentedCompressor alreadyInstrumented) return alreadyInstrumented;
            return new InstrumentedCompressor(compressor, algorithmName);
        }
    }
}
