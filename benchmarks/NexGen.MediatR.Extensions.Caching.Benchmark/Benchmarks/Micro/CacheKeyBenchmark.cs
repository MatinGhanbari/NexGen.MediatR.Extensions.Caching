using BenchmarkDotNet.Attributes;
using NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Micro;

[Config(typeof(ShortRunConfig))]
public class CacheKeyBenchmark
{
    [Params(1, 10, 100)]
    public int PropertyCount { get; set; }

    private VariablePropertyRequest _request = null!;
    private readonly CachedQuery _smallRequest = new(42);

    [GlobalSetup]
    public void GlobalSetup()
    {
        _request = new VariablePropertyRequest
        {
            Values = Enumerable.Range(0, PropertyCount).ToDictionary(i => $"P{i}", i => i)
        };
    }

    [Benchmark]
    public string SmallRequest() => RequestOutputCacheHelper.GetCacheKey(_smallRequest);

    [Benchmark]
    public string VariableRequest() => RequestOutputCacheHelper.GetCacheKey(_request);
}
