using BenchmarkDotNet.Attributes;
using NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;
using NexGen.MediatR.Extensions.Caching.Containers;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Micro;

[Config(typeof(ShortRunConfig))]
public class ContainerBenchmark
{
    [Params(1, 10, 100)]
    public int EntryCount { get; set; }

    private RequestOutputCacheContainer _container = null!;
    private string[] _tags = null!;
    private readonly string _requestTypeName = typeof(CachedQuery).FullName!;

    [IterationSetup]
    public void IterationSetup()
    {
        _container = new RequestOutputCacheContainer();
        _tags = Enumerable.Range(0, EntryCount).Select(i => $"tag-{i}").ToArray();

        for (var i = 0; i < EntryCount; i++)
        {
            _container.UpdateContainerAsync<CachedQuery>(
                    tags: ["User"],
                    cacheKey: $"key-{i}",
                    responseType: typeof(string))
                .GetAwaiter()
                .GetResult();
        }
    }

    [Benchmark]
    public Task UpdateContainer() =>
        _container.UpdateContainerAsync<CachedQuery>(
            tags: _tags,
            cacheKey: "key-update",
            responseType: typeof(string));

    [Benchmark]
    public Task RemoveRequestTypes() =>
        _container.RemoveRequestTypesAsync([_requestTypeName]);
}
