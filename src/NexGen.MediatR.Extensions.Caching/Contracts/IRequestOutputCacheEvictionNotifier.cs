namespace NexGen.MediatR.Extensions.Caching.Contracts;

/// <summary>
/// Notifies other hosts that cache tags have been invalidated locally.
/// Implemented by Redis and Garnet providers when distributed eviction is enabled.
/// Memory cache does not register a notifier.
/// </summary>
public interface IRequestOutputCacheEvictionNotifier
{
    /// <summary>
    /// Publishes an eviction notification for the given tags.
    /// Implementations should treat this as best-effort and must not throw on transport failures.
    /// </summary>
    /// <param name="tags">Normalized cache tags to invalidate on other hosts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task NotifyAsync(IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default);
}
