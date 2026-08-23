using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Pipeline;

[Config(typeof(ShortRunConfig))]
public class CachePipelineBenchmark
{
    private const string CachedTag = "User";
    private const int ExpirationInSeconds = 3600;

    private ServiceProvider _provider = null!;
    private IServiceScope _scope = null!;
    private IMediator _mediator = null!;
    private IRequestOutputCache<CachedQuery, string> _cache = null!;
    private readonly CachedQuery _hitRequest = new(1);
    private readonly UncachedQuery _uncachedRequest = new(1);
    private int _missId;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _provider = (ServiceProvider)BenchmarkServiceProviderFactory.Create(BenchmarkServiceProviderFactory.Memory);
        _scope = _provider.CreateScope();
        _mediator = _scope.ServiceProvider.GetRequiredService<IMediator>();
        _cache = _scope.ServiceProvider.GetRequiredService<IRequestOutputCache<CachedQuery, string>>();
        _mediator.Send(_hitRequest).GetAwaiter().GetResult();
        _missId = 1_000;
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _scope.Dispose();
        _provider.Dispose();
    }

    [Benchmark(Baseline = true)]
    public Task<string> NoAttribute() => _mediator.Send(_uncachedRequest);

    [Benchmark]
    public Task<string> CacheHit() => _mediator.Send(_hitRequest);

    [Benchmark]
    public Task<string> CacheMiss()
    {
        var id = Interlocked.Increment(ref _missId);
        return _mediator.Send(new CachedQuery(id));
    }

    [Benchmark]
    public Task CacheSet() =>
        _cache.SetAsync(_hitRequest, "result", [CachedTag], ExpirationInSeconds);
}
