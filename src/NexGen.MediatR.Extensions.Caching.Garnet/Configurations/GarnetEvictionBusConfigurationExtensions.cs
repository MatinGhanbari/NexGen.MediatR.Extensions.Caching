using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;
using NexGen.MediatR.Extensions.Caching.Garnet.Eviction;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Configurations;

/// <summary>
/// Extension methods for configuring Garnet Pub/Sub as the MediatR output-cache eviction bus.
/// </summary>
public static class GarnetEvictionBusConfigurationExtensions
{
    /// <summary>
    /// Registers Garnet Pub/Sub eviction publishing and subscription on a query host,
    /// and starts <see cref="RequestOutputCacheEvictionHostedService"/>.
    /// </summary>
    /// <param name="options">Cache configuration options.</param>
    /// <param name="connectionString">Garnet connection string.</param>
    public static void UseGarnetEvictionBus(
        this RequestOutputCacheConfigurationOption options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        RegisterConnection(options.Services, connectionString);
        options.Services.TryAddSingleton<IRequestOutputCacheEvictionPublisher, GarnetRequestOutputCacheEvictionPublisher>();
        options.Services.TryAddSingleton<IRequestOutputCacheEvictionSubscriber, GarnetRequestOutputCacheEvictionSubscriber>();
        options.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, RequestOutputCacheEvictionHostedService>());
    }

    /// <summary>
    /// Registers Garnet Pub/Sub eviction publishing on a command host (no subscriber).
    /// </summary>
    /// <param name="options">Eviction configuration options.</param>
    /// <param name="connectionString">Garnet connection string.</param>
    public static void UseGarnetEvictionBus(
        this RequestOutputCacheEvictionConfigurationOption options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        RegisterConnection(options.Services, connectionString);
        options.Services.TryAddSingleton<IRequestOutputCacheEvictionPublisher, GarnetRequestOutputCacheEvictionPublisher>();
    }

    private static void RegisterConnection(IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(ErrorMessages.EmptyConnectionString, nameof(connectionString));

        services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
    }
}
