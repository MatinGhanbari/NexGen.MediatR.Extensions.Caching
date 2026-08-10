namespace NexGen.MediatR.Extensions.Caching.Messages;

/// <summary>
/// Stable payload published on an eviction bus (in-process, Redis, Rabbit, Kafka, etc.)
/// to invalidate MediatR output-cache entries by tag.
/// </summary>
public sealed class RequestOutputCacheEvictionMessage
{
    /// <summary>
    /// Cache tags to evict. For EF auto-evict these are typically entity CLR type names
    /// (for example <c>nameof(User)</c>).
    /// </summary>
    public string[] Tags { get; init; } = [];
}
