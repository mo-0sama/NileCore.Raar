using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace NileCore.Raar.Caching.HybridCache
{
    /// <summary>
    /// Extension methods for registering <see cref="HybridCacheHealthCheck"/> with <see cref="IHealthChecksBuilder"/>.
    /// </summary>
    public static class HybridCacheHealthCheckExtensions
    {
        /// <summary>
        /// Adds a health check for <see cref="HybridCacheService"/> tiers to the health checks builder.
        /// </summary>
        public static IHealthChecksBuilder AddRaarHybridCacheHealthCheck(
            this IHealthChecksBuilder builder,
            string name = "hybrid-cache",
            HealthStatus? failureStatus = null,
            IEnumerable<string>? tags = null,
            TimeSpan? timeout = null)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            return builder.AddCheck<HybridCacheHealthCheck>(
                name,
                failureStatus,
                tags ?? new[] { "cache", "hybrid-cache", "ready" },
                timeout);
        }
    }
}
