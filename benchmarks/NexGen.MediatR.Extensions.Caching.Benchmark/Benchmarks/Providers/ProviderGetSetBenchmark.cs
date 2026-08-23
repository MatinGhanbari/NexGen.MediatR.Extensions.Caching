using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Providers;

public abstract class ProviderGetSetBenchmark
{
    private const int ExpirationInSeconds = 3600;

    protected abstract string ProviderName { get; }

    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IRequestOutputCache<CachedQuery, string> _cache = null!;
    private readonly CachedQuery _hitRequest = new(1);
    private int _missId;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _provider = (ServiceProvider)BenchmarkServiceProviderFactory.Create(ProviderName);
        _scope = _provider.CreateScope();
        _cache = _scope.ServiceProvider.GetRequiredService<IRequestOutputCache<CachedQuery, string>>();
        _cache.SetAsync(_hitRequest, "cached", ["User"], ExpirationInSeconds).GetAwaiter().GetResult();
        _missId = 1_000_000;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Benchmark]
    public Task GetHit() => _cache.GetAsync(_hitRequest);

    [Benchmark]
    public Task SetMiss()
    {
        var id = Interlocked.Increment(ref _missId);
        return _cache.SetAsync(new CachedQuery(id), "value", ["User"], ExpirationInSeconds);
    }
}

[Config(typeof(ShortRunConfig))]
public class MemoryProviderGetSetBenchmark : ProviderGetSetBenchmark
{
    protected override string ProviderName => BenchmarkServiceProviderFactory.Memory;
}

[Config(typeof(ShortRunConfig))]
public class RedisProviderGetSetBenchmark : ProviderGetSetBenchmark
{
    protected override string ProviderName => BenchmarkServiceProviderFactory.Redis;
}

[Config(typeof(ShortRunConfig))]
public class GarnetProviderGetSetBenchmark : ProviderGetSetBenchmark
{
    protected override string ProviderName => BenchmarkServiceProviderFactory.Garnet;
}
