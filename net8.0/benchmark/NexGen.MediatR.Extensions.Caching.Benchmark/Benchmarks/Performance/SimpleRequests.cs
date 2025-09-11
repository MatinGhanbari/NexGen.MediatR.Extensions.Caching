using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Performance;


[RequestOutputCache(tags: [nameof(SimpleCachedRequest)], expirationInSeconds: int.MaxValue)]
public record SimpleCachedRequest : IRequest<string>
{
    public string Title { get; set; }
    public int Code { get; set; }

    public SimpleCachedRequest(string title)
    {
        Title = title;
        Code = new Random().Next(0, 10);
    }
}


public record SimpleNotCachedRequest : IRequest<string>
{
    public string Title { get; set; }
    public int Code { get; set; }

    public SimpleNotCachedRequest(string title)
    {
        Title = title;
        Code = new Random().Next(0, 10);
    }
}