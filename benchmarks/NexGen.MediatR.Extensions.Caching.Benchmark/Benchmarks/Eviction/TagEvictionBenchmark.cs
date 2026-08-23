using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Eviction;

[Config(typeof(ShortRunConfig))]
public class TagEvictionBenchmark
{
    private const int ExpirationInSeconds = 3600;

    [Params(10, 100, 1000)]
    public int EntryCount { get; set; }

    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IRequestOutputCache<CachedQuery, string> _userCache = null!;
    private IRequestOutputCache<OrderQuery, string> _orderCache = null!;
    private IRequestOutputCache<ProductQuery, string> _productCache = null!;
    private IRequestOutputCacheInvalidator _invalidator = null!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _provider = (ServiceProvider)BenchmarkServiceProviderFactory.Create(BenchmarkServiceProviderFactory.Memory);
        _scope = _provider.CreateScope();
        var sp = _scope.ServiceProvider;
        _userCache = sp.GetRequiredService<IRequestOutputCache<CachedQuery, string>>();
        _orderCache = sp.GetRequiredService<IRequestOutputCache<OrderQuery, string>>();
        _productCache = sp.GetRequiredService<IRequestOutputCache<ProductQuery, string>>();
        _invalidator = sp.GetRequiredService<IRequestOutputCacheInvalidator>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _invalidator.FlushAll().GetAwaiter().GetResult();

        for (var i = 0; i < EntryCount; i++)
        {
            _userCache.SetAsync(new CachedQuery(i), "user", ["User"], ExpirationInSeconds)
                .GetAwaiter()
                .GetResult();
            _orderCache.SetAsync(new OrderQuery(i), "order", ["Order"], ExpirationInSeconds)
                .GetAwaiter()
                .GetResult();
            _productCache.SetAsync(new ProductQuery(i), "product", ["Product"], ExpirationInSeconds)
                .GetAwaiter()
                .GetResult();
        }
    }

    [Benchmark]
    public Task EvictSingleTag() => _invalidator.EvictByTagsAsync(["User"]);

    [Benchmark]
    public Task EvictManyTags() => _invalidator.EvictByTagsAsync(["User", "Order", "Product"]);
}
