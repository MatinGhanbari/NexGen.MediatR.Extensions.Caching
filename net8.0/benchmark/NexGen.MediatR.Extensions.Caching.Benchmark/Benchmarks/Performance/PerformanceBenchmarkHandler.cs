using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Performance;

[MemoryDiagnoser]
public class PerformanceBenchmarkHandler
{
    private readonly IMediator _mediator;
    private readonly string _requestTitle = "PerformanceBench";

    public PerformanceBenchmarkHandler()
    {
        var services = new ServiceCollection();
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
    public async Task<object?> BenchmarkGetUserProfile()
    {
        return await _mediator.Send(new SimpleCachedRequest() { Title = _requestTitle });
    }

    [Benchmark]
    public async Task<object?> BenchmarkGetOrderSummary()
    {
        return await _mediator.Send(new SimpleNotCachedRequest() { Title = _requestTitle });
    }
}