using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.Contracts;

/// <summary>
/// Publishes cache eviction messages so other hosts or DI containers can invalidate cached responses.
/// </summary>
public interface IRequestOutputCacheEvictionPublisher
{
    /// <summary>
    /// Publishes an eviction message containing the tags to invalidate.
    /// </summary>
    /// <param name="message">The eviction message.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task PublishAsync(RequestOutputCacheEvictionMessage message, CancellationToken cancellationToken = default);
}
