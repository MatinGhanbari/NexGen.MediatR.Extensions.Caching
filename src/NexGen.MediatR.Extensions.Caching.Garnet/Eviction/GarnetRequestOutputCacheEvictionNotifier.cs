using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;
using NexGen.MediatR.Extensions.Caching.Helpers;
using NexGen.MediatR.Extensions.Caching.Messages;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Eviction;

/// <summary>
/// Publishes cache eviction notifications over Garnet Pub/Sub.
/// </summary>
internal sealed class GarnetRequestOutputCacheEvictionNotifier : IRequestOutputCacheEvictionNotifier
{
    private readonly IConnectionMultiplexer _connection;
    private readonly GarnetEvictionOptions _options;
    private readonly RequestOutputCacheEvictionNode _node;
    private readonly ILogger<GarnetRequestOutputCacheEvictionNotifier> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GarnetRequestOutputCacheEvictionNotifier"/> class.
    /// </summary>
    public GarnetRequestOutputCacheEvictionNotifier(
        IConnectionMultiplexer connection,
        GarnetEvictionOptions options,
        RequestOutputCacheEvictionNode node,
        IServiceProvider serviceProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _node = node ?? throw new ArgumentNullException(nameof(node));
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _logger = serviceProvider.GetService<ILogger<GarnetRequestOutputCacheEvictionNotifier>>()
            ?? NullLogger<GarnetRequestOutputCacheEvictionNotifier>.Instance;
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
            _logger.LogWarning(ex, "Failed to publish Garnet cache eviction notification for tags {Tags}.", tags);
        }
    }
}
