using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.Eviction;

/// <summary>
/// Evicts matching cache entries in the current process, then notifies other hosts when a
/// <see cref="IRequestOutputCacheEvictionNotifier"/> is registered (Redis/Garnet Pub/Sub).
/// </summary>
public sealed class RequestOutputCacheEvictionDispatcher
{
    private readonly IRequestOutputCacheInvalidator _invalidator;
    private readonly IRequestOutputCacheEvictionNotifier? _notifier;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheEvictionDispatcher"/> class.
    /// </summary>
    /// <param name="invalidator">Local tag invalidator for the configured cache provider.</param>
    /// <param name="serviceProvider">Used to resolve an optional distributed eviction notifier.</param>
    public RequestOutputCacheEvictionDispatcher(
        IRequestOutputCacheInvalidator invalidator,
        IServiceProvider serviceProvider)
    {
        _invalidator = invalidator ?? throw new ArgumentNullException(nameof(invalidator));
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _notifier = serviceProvider.GetService<IRequestOutputCacheEvictionNotifier>();
    }

    /// <summary>
    /// Evicts the given tags locally and, when a notifier is registered, publishes them to other hosts.
    /// Empty, whitespace, and duplicate tags are ignored. All remaining tags are sent in one call.
    /// </summary>
    /// <param name="tags">Cache tags to invalidate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A failed result when local eviction fails; notify failures are swallowed by the notifier.</returns>
    public async Task<Result> DispatchAsync(
        IEnumerable<string>? tags,
        CancellationToken cancellationToken = default)
    {
        var normalized = RequestOutputCacheTagNormalizer.Normalize(tags);
        if (normalized.Length == 0)
            return Result.Ok();

        var result = await _invalidator.EvictByTagsAsync(normalized, cancellationToken).ConfigureAwait(false);
        if (result.IsFailed)
            return result;

        if (_notifier is null)
            return Result.Ok();

        await _notifier.NotifyAsync(normalized, cancellationToken).ConfigureAwait(false);
        return Result.Ok();
    }
}
