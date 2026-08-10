using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Eviction;

/// <summary>
/// Background service that listens for eviction bus messages and applies them via
/// <see cref="IRequestOutputCacheInvalidator"/>.
/// </summary>
public sealed class RequestOutputCacheEvictionHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IRequestOutputCacheEvictionSubscriber _subscriber;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheEvictionHostedService"/> class.
    /// </summary>
    public RequestOutputCacheEvictionHostedService(
        IServiceScopeFactory scopeFactory,
        IRequestOutputCacheEvictionSubscriber subscriber)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _subscriber = subscriber ?? throw new ArgumentNullException(nameof(subscriber));
    }

    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return _subscriber.SubscribeAsync(async (message, cancellationToken) =>
        {
            if (message.Tags is null || message.Tags.Length == 0)
                return;

            using var scope = _scopeFactory.CreateScope();
            var invalidator = scope.ServiceProvider.GetRequiredService<IRequestOutputCacheInvalidator>();
            await invalidator.EvictByTagsAsync(message.Tags, cancellationToken).ConfigureAwait(false);
        }, stoppingToken);
    }
}
