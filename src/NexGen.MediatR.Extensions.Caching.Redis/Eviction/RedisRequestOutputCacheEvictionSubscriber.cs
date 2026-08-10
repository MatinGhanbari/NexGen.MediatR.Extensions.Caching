using System.Threading.Channels;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Messages;
using NexGen.MediatR.Extensions.Caching.Redis.Constants;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Redis.Eviction;

/// <summary>
/// Subscribes to Redis Pub/Sub eviction messages.
/// </summary>
public sealed class RedisRequestOutputCacheEvictionSubscriber : IRequestOutputCacheEvictionSubscriber
{
    private readonly IConnectionMultiplexer _connection;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisRequestOutputCacheEvictionSubscriber"/> class.
    /// </summary>
    /// <param name="connection">Redis connection multiplexer.</param>
    public RedisRequestOutputCacheEvictionSubscriber(IConnectionMultiplexer connection)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
    }

    /// <inheritdoc />
    public async Task SubscribeAsync(
        Func<RequestOutputCacheEvictionMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(handler);

        var channel = Channel.CreateUnbounded<RequestOutputCacheEvictionMessage>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        var subscriber = _connection.GetSubscriber();
        var redisChannel = RedisChannel.Literal(CacheKeys.EvictionChannel);

        await subscriber.SubscribeAsync(redisChannel, (_, value) =>
        {
            if (value.IsNullOrEmpty)
                return;

            try
            {
                var message = JsonConvert.DeserializeObject<RequestOutputCacheEvictionMessage>(value!);
                if (message?.Tags is { Length: > 0 })
                    channel.Writer.TryWrite(message);
            }
            catch (JsonException)
            {
                // Ignore malformed payloads.
            }
        }).ConfigureAwait(false);

        try
        {
            await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                await handler(message, cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            await subscriber.UnsubscribeAsync(redisChannel).ConfigureAwait(false);
            channel.Writer.TryComplete();
        }
    }
}
