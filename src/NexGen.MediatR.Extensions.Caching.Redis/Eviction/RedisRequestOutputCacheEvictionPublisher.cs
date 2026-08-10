using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Messages;
using NexGen.MediatR.Extensions.Caching.Redis.Constants;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Redis.Eviction;

/// <summary>
/// Publishes eviction messages over Redis Pub/Sub.
/// </summary>
public sealed class RedisRequestOutputCacheEvictionPublisher : IRequestOutputCacheEvictionPublisher
{
    private readonly IConnectionMultiplexer _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisRequestOutputCacheEvictionPublisher"/> class.
    /// </summary>
    /// <param name="connection">Redis connection multiplexer.</param>
    public RedisRequestOutputCacheEvictionPublisher(IConnectionMultiplexer connection)
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
