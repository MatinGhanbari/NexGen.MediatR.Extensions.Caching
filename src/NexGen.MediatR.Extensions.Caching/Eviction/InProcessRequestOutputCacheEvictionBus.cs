using System.Threading.Channels;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.Eviction;

/// <summary>
/// In-process eviction bus backed by an unbounded channel.
/// Register the same instance in both command and query DI containers when co-deployed.
/// </summary>
public sealed class InProcessRequestOutputCacheEvictionBus
    : IRequestOutputCacheEvictionPublisher, IRequestOutputCacheEvictionSubscriber
{
    private readonly Channel<RequestOutputCacheEvictionMessage> _channel =
        Channel.CreateUnbounded<RequestOutputCacheEvictionMessage>(new UnboundedChannelOptions
        {
            SingleReader = false,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

    /// <inheritdoc />
    public async Task PublishAsync(
        RequestOutputCacheEvictionMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Tags is null || message.Tags.Length == 0)
            return;

        await _channel.Writer.WriteAsync(message, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(
        Func<RequestOutputCacheEvictionMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await handler(message, cancellationToken).ConfigureAwait(false);
        }
    }
}
