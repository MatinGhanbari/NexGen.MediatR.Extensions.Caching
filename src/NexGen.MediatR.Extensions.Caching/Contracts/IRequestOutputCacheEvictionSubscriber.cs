using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.Contracts;

/// <summary>
/// Subscribes to cache eviction messages from a bus or in-process channel.
/// </summary>
public interface IRequestOutputCacheEvictionSubscriber
{
    /// <summary>
    /// Starts listening for eviction messages and invokes <paramref name="handler"/> for each message.
    /// Typically runs until <paramref name="cancellationToken"/> is cancelled.
    /// </summary>
    /// <param name="handler">Callback that processes each eviction message.</param>
    /// <param name="cancellationToken">Cancellation token that stops the subscription.</param>
    Task SubscribeAsync(
        Func<RequestOutputCacheEvictionMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default);
}
