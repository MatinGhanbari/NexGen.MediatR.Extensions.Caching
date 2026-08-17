using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;
using NexGen.MediatR.Extensions.Caching.Helpers;
using NexGen.MediatR.Extensions.Caching.Messages;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Eviction;

/// <summary>
/// Subscribes to Garnet Pub/Sub eviction notifications and applies them locally.
/// </summary>
internal sealed class GarnetRequestOutputCacheEvictionListener : BackgroundService
{
    private readonly IConnectionMultiplexer _connection;
    private readonly GarnetEvictionOptions _options;
    private readonly RequestOutputCacheEvictionNode _node;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<GarnetRequestOutputCacheEvictionListener> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GarnetRequestOutputCacheEvictionListener"/> class.
    /// </summary>
    public GarnetRequestOutputCacheEvictionListener(
        IConnectionMultiplexer connection,
        GarnetEvictionOptions options,
        RequestOutputCacheEvictionNode node,
        IServiceScopeFactory scopeFactory,
        IServiceProvider serviceProvider)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _node = node ?? throw new ArgumentNullException(nameof(node));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        ArgumentNullException.ThrowIfNull(serviceProvider);
        _logger = serviceProvider.GetService<ILogger<GarnetRequestOutputCacheEvictionListener>>()
            ?? NullLogger<GarnetRequestOutputCacheEvictionListener>.Instance;
    }

    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mailbox = Channel.CreateUnbounded<RequestOutputCacheEvictionNotification>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false,
            AllowSynchronousContinuations = false
        });

        var subscriber = _connection.GetSubscriber();
        var redisChannel = RedisChannel.Literal(_options.Channel);

        await subscriber.SubscribeAsync(redisChannel, (_, value) =>
        {
            if (value.IsNullOrEmpty)
                return;

            var notification = RequestOutputCacheEvictionNotificationFormatter.TryDeserialize(value!);
            if (notification is null)
                return;

            if (string.Equals(notification.SenderId, _node.Id, StringComparison.Ordinal))
                return;

            mailbox.Writer.TryWrite(notification);
        }).ConfigureAwait(false);

        try
        {
            await foreach (var notification in mailbox.Reader.ReadAllAsync(stoppingToken).ConfigureAwait(false))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var invalidator = scope.ServiceProvider.GetRequiredService<IRequestOutputCacheInvalidator>();
                    await invalidator.EvictByTagsAsync(notification.Tags, stoppingToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Failed to apply Garnet cache eviction notification for tags {Tags}.", notification.Tags);
                }
            }
        }
        finally
        {
            try
            {
                await subscriber.UnsubscribeAsync(redisChannel).ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Failed to unsubscribe from Garnet eviction channel {Channel}.", _options.Channel);
            }

            mailbox.Writer.TryComplete();
        }
    }
}
