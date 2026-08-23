using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;

[RequestOutputCache(tags: ["User"], expirationInSeconds: 3600)]
public sealed record CachedQuery(int Id) : IRequest<string>;

[RequestOutputCache(tags: ["Order"], expirationInSeconds: 3600)]
public sealed record OrderQuery(int Id) : IRequest<string>;

[RequestOutputCache(tags: ["Product"], expirationInSeconds: 3600)]
public sealed record ProductQuery(int Id) : IRequest<string>;

public sealed record UncachedQuery(int Id) : IRequest<string>;

public sealed class VariablePropertyRequest
{
    public Dictionary<string, int> Values { get; init; } = [];
}
