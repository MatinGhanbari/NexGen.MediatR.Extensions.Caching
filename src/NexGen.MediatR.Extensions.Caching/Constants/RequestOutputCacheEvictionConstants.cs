namespace NexGen.MediatR.Extensions.Caching.Constants;

/// <summary>
/// Suggested conventions for publishing <see cref="Messages.RequestOutputCacheEvictionMessage"/>
/// on an external message bus.
/// </summary>
public static class RequestOutputCacheEvictionConstants
{
    /// <summary>
    /// Recommended topic / exchange / subject name when wiring RabbitMQ, Kafka, MassTransit, or similar.
    /// </summary>
    public const string DefaultBusTopic = "mediatr.outputcache.evict";
}
