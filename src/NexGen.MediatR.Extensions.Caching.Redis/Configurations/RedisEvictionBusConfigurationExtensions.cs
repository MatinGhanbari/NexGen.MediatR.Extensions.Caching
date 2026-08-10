using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;
using NexGen.MediatR.Extensions.Caching.Redis.Eviction;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Redis.Configurations;

/// <summary>
/// Extension methods for configuring Redis Pub/Sub as the MediatR output-cache eviction bus.
/// </summary>
public static class RedisEvictionBusConfigurationExtensions
{
    /// <summary>
    /// Registers Redis Pub/Sub eviction publishing and subscription on a query host,
    /// and starts <see cref="RequestOutputCacheEvictionHostedService"/>.
    /// </summary>
    /// <param name="options">Cache configuration options.</param>
    /// <param name="connectionString">Redis connection string.</param>
    public static void UseRedisEvictionBus(
        this RequestOutputCacheConfigurationOption options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        RegisterConnection(options.Services, connectionString);
        options.Services.TryAddSingleton<IRequestOutputCacheEvictionPublisher, RedisRequestOutputCacheEvictionPublisher>();
        options.Services.TryAddSingleton<IRequestOutputCacheEvictionSubscriber, RedisRequestOutputCacheEvictionSubscriber>();
        options.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, RequestOutputCacheEvictionHostedService>());
    }

    /// <summary>
    /// Registers Redis Pub/Sub eviction publishing on a command host (no subscriber).
    /// </summary>
    /// <param name="options">Eviction configuration options.</param>
    /// <param name="connectionString">Redis connection string.</param>
    public static void UseRedisEvictionBus(
        this RequestOutputCacheEvictionConfigurationOption options,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);
        RegisterConnection(options.Services, connectionString);
        options.Services.TryAddSingleton<IRequestOutputCacheEvictionPublisher, RedisRequestOutputCacheEvictionPublisher>();
    }

    private static void RegisterConnection(IServiceCollection services, string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(ErrorMessages.EmptyConnectionString, nameof(connectionString));

        services.TryAddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(connectionString));
    }
}
