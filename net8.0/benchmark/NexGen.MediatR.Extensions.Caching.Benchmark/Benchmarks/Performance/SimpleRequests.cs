using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Performance;


public record SimpleNotCachedRequest : IRequest<bool>, IRequest<Task>
{
    public string Title { get; set; }
}


[RequestOutputCache(tags: [nameof(SimpleCachedRequest)], expirationInSeconds: int.MaxValue)]
public record SimpleCachedRequest : IRequest<bool>, IRequest<string>, IRequest<Task>
{
    public string Title { get; set; }
}