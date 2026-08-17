namespace NexGen.MediatR.Extensions.Caching.Eviction;

/// <summary>
/// Process-wide identity used to ignore this host's own eviction Pub/Sub messages.
/// </summary>
public sealed class RequestOutputCacheEvictionNode
{
    /// <summary>
    /// Gets the unique identifier for this host process.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString("N");
}
