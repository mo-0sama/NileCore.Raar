using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace NileCore.Raar.Caching.HybridCache
{
    /// <summary>
    /// Health check for verifying the operational status of L1 in-memory and L2 distributed cache tiers.
    /// </summary>
    public class HybridCacheHealthCheck : IHealthCheck
    {
        private const string HealthCanaryKey = "__raar_health_canary__";
        private readonly IMemoryCache _memoryCache;
        private readonly IDistributedCache _distributedCache;
        private readonly HybridCacheOptions _options;

        public HybridCacheHealthCheck(
            IMemoryCache memoryCache,
            IDistributedCache distributedCache,
            IOptions<HybridCacheOptions> options)
        {
            _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
            _distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
            _options = options?.Value ?? new HybridCacheOptions();
        }

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            var data = new Dictionary<string, object>
            {
                ["MemoryCacheEnabled"] = _options.EnableMemoryCache,
                ["DistributedCacheEnabled"] = _options.EnableDistributedCache
            };

            try
            {
                // 1. Verify L1 Memory Cache
                if (_options.EnableMemoryCache)
                {
                    const string memoryTestKey = "__raar_l1_test__";
                    _memoryCache.Set(memoryTestKey, "ok", TimeSpan.FromSeconds(5));
                    if (!_memoryCache.TryGetValue(memoryTestKey, out string? memVal) || memVal != "ok")
                    {
                        return HealthCheckResult.Degraded("L1 MemoryCache failed read/write probe.", data: data);
                    }
                    _memoryCache.Remove(memoryTestKey);
                    data["L1MemoryStatus"] = "Healthy";
                }

                // 2. Verify L2 Distributed Cache
                if (_options.EnableDistributedCache)
                {
                    string canary = Guid.NewGuid().ToString("N");
                    var entryOptions = new DistributedCacheEntryOptions
                    {
                        AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10)
                    };

                    await _distributedCache.SetStringAsync(HealthCanaryKey, canary, entryOptions, cancellationToken).ConfigureAwait(false);
                    var readBack = await _distributedCache.GetStringAsync(HealthCanaryKey, cancellationToken).ConfigureAwait(false);

                    if (readBack != canary)
                    {
                        return HealthCheckResult.Degraded("L2 DistributedCache failed probe value verification.", data: data);
                    }

                    await _distributedCache.RemoveAsync(HealthCanaryKey, cancellationToken).ConfigureAwait(false);
                    data["L2DistributedStatus"] = "Healthy";
                }

                return HealthCheckResult.Healthy("HybridCache is fully operational.", data);
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy($"HybridCache health check encountered an error: {ex.Message}", ex, data);
            }
        }
    }
}
