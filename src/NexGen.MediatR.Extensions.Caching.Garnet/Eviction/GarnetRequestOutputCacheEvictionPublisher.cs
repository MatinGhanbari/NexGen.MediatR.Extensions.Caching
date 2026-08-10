using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Garnet.Constants;
using NexGen.MediatR.Extensions.Caching.Messages;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Eviction;

/// <summary>
/// Publishes eviction messages over Garnet (Redis-protocol) Pub/Sub.
/// </summary>
public sealed class GarnetRequestOutputCacheEvictionPublisher : IRequestOutputCacheEvictionPublisher
{
    private readonly IConnectionMultiplexer _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="GarnetRequestOutputCacheEvictionPublisher"/> class.
    /// </summary>
    /// <param name="connection">Garnet/Redis connection multiplexer.</param>
    public GarnetRequestOutputCacheEvictionPublisher(IConnectionMultiplexer connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task PublishAsync(
        RequestOutputCacheEvictionMessage message,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (message.Tags is null || message.Tags.Length == 0)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var payload = JsonConvert.SerializeObject(message);
        var subscriber = _connection.GetSubscriber();
        await subscriber.PublishAsync(RedisChannel.Literal(CacheKeys.EvictionChannel), payload).ConfigureAwait(false);
    }
}
