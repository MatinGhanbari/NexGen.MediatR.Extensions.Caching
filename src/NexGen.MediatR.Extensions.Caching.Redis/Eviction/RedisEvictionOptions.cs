namespace NexGen.MediatR.Extensions.Caching.Redis.Eviction;

internal sealed class RedisEvictionOptions
{
    internal required string Channel { get; init; }
}
