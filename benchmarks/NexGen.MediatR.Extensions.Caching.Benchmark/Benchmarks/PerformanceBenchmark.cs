using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Performance;
using NexGen.MediatR.Extensions.Caching.Configurations;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks;

[MemoryDiagnoser]
public class PerformanceBenchmark
{
    private readonly IMediator _mediator;
    private readonly string _requestTitle = "PerformanceBench";

    public PerformanceBenchmark()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<SimpleRequestsHandlers>();
        });

        services.AddMediatROutputCache(opt =>
        {
            opt.UseMemoryCache();
        });

        var provider = services.BuildServiceProvider();
        _mediator = provider.GetRequiredService<IMediator>();
    }

    [Benchmark]
    public async Task<object?> CachedRequest()
    {
        return await _mediator.Send(new SimpleCachedRequest(_requestTitle));
    }

    [Benchmark]
    public async Task<object?> NotCachedRequest()
    {
        return await _mediator.Send(new SimpleNotCachedRequest(_requestTitle));
    }
}