using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;
using NexGen.MediatR.Extensions.Caching.Helpers;
using NexGen.MediatR.Extensions.Caching.Messages;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Redis.Eviction;

/// <summary>
/// Publishes cache eviction notifications over Redis Pub/Sub.
/// </summary>
internal sealed class RedisRequestOutputCacheEvictionNotifier : IRequestOutputCacheEvictionNotifier
{
    private readonly IConnectionMultiplexer _connection;
    private readonly RedisEvictionOptions _options;
    private readonly RequestOutputCacheEvictionNode _node;
    private readonly ILogger<RedisRequestOutputCacheEvictionNotifier> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisRequestOutputCacheEvictionNotifier"/> class.
    /// </summary>
    public RedisRequestOutputCacheEvictionNotifier(
        IConnectionMultiplexer connection,
        RedisEvictionOptions options,
        RequestOutputCacheEvictionNode node,
        IServiceProvider serviceProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _node = node ?? throw new ArgumentNullException(nameof(node));
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _logger = serviceProvider.GetService<ILogger<RedisRequestOutputCacheEvictionNotifier>>()
            ?? NullLogger<RedisRequestOutputCacheEvictionNotifier>.Instance;
    }

    /// <inheritdoc />
    public async Task NotifyAsync(
        IReadOnlyCollection<string> tags,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tags);

        if (tags.Count == 0)
            return;

        cancellationToken.ThrowIfCancellationRequested();

        var payload = RequestOutputCacheEvictionNotificationFormatter.Serialize(
            new RequestOutputCacheEvictionNotification
            {
                Tags = tags as string[] ?? tags.ToArray(),
                SenderId = _node.Id,
                TimestampUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

        try
        {
            var subscriber = _connection.GetSubscriber();
            await subscriber.PublishAsync(RedisChannel.Literal(_options.Channel), payload).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Failed to publish Redis cache eviction notification for tags {Tags}.", tags);
        }
    }
}
