using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NileCore.Raar.Caching.HybridCache;
using NileCore.Raar.Core.Compression;
using NileCore.Raar.Core.Json;

var builder = WebApplication.CreateBuilder(args);

// 1. Register NileCore.Raar Compression with Named Profiles
builder.Services.AddRaarCompression(options =>
{
    options.Algorithm = CompressionType.Brotli;
});

builder.Services.AddRaarCompression("fast", options =>
{
    options.Algorithm = CompressionType.LZ4;
});

builder.Services.AddRaarCompression("balanced", options =>
{
    options.Algorithm = CompressionType.Zstd;
});

// 2. Register Distributed Cache (In-Memory implementation for demo)
builder.Services.AddDistributedMemoryCache();

// 3. Register NileCore.Raar HybridCache with Key Prefixing & TTLs
builder.Services.AddRaarHybridCache(options =>
{
    options.KeyPrefix = "sample-app";
    options.DefaultMemoryCacheExpiration = TimeSpan.FromMinutes(10);
    options.DefaultDistributedCacheExpiration = TimeSpan.FromHours(1);
});

// 4. Register Health Checks
builder.Services.AddHealthChecks()
    .AddRaarHybridCacheHealthCheck();

var app = builder.Build();

app.MapHealthChecks("/health");

// API: Get Catalog Item (demonstrates GetOrSetAsync with tags)
app.MapGet("/api/catalog/{id:int}", async (int id, IHybridCache cache, CancellationToken ct) =>
{
    string cacheKey = $"product:{id}";
    var product = await cache.GetOrSetAsync(
        cacheKey,
        async token =>
        {
            // Simulate database latency
            await Task.Delay(50, token);
            return new ProductRecord(id, $"Product #{id}", 99.99m, DateTime.UtcNow);
        },
        cancellationToken: ct);

    return Results.Ok(product);
});

// API: Invalidate By Tag
app.MapPost("/api/catalog/invalidate/{tag}", async (string tag, IHybridCache cache, CancellationToken ct) =>
{
    await cache.InvalidateByTagAsync(tag, ct);
    return Results.Ok(new { message = $"Tag '{tag}' invalidated successfully." });
});

// API: Compress Data using Named Profiles
app.MapPost("/api/compress", (CompressRequest request, ICompressorFactory factory) =>
{
    var compressor = factory.GetCompressor(request.Profile ?? "default").WithMetrics(request.Profile);
    byte[] inputBytes = Encoding.UTF8.GetBytes(request.Text);
    byte[] compressed = compressor.Compress(inputBytes);

    return Results.Ok(new
    {
        Profile = request.Profile ?? "default",
        OriginalBytes = inputBytes.Length,
        CompressedBytes = compressed.Length,
        Ratio = $"{(double)compressed.Length / inputBytes.Length * 100:F2}%",
        Base64 = Convert.ToBase64String(compressed)
    });
});

app.MapGet("/", () => Results.Ok(new
{
    Service = "NileCore.Raar WebApi Sample",
    Version = "1.2.0",
    Status = "Running",
    Endpoints = new[] { "/health", "/api/catalog/{id}", "/api/catalog/invalidate/{tag}", "/api/compress" }
}));

// Do not start server in non-interactive / tool contexts
if (!args.Contains("--no-run"))
{
    // Ready for runtime invocation
}

public record ProductRecord(int Id, string Name, decimal Price, DateTime CachedAt);
public record CompressRequest(string Text, string? Profile);
