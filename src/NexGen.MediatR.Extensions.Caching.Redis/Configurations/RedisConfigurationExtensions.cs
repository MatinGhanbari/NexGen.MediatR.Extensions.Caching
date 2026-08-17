using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;
using NexGen.MediatR.Extensions.Caching.Helpers;
using NexGen.MediatR.Extensions.Caching.Redis.Containers;
using NexGen.MediatR.Extensions.Caching.Redis.Eviction;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Redis.Configurations;

/// <summary>
/// Provides extension methods to configure Redis caching for MediatR requests.
/// </summary>
public static class RedisConfigurationExtensions
{
    /// <summary>
    /// Configures the library to use Redis cache for MediatR request responses.
    /// Distributed tag eviction over Redis Pub/Sub is enabled by default.
    /// </summary>
    /// <param name="options">The output cache configuration options.</param>
    /// <param name="connectionString">The connection string for the Redis server.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="connectionString"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a cache type has already been configured.</exception>
    public static void UseRedisCache(this RequestOutputCacheConfigurationOption options, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(ErrorMessages.EmptyConnectionString, nameof(connectionString));

        options.UseRedisCache(redis => redis.ConnectionString = connectionString);
    }

    /// <summary>
    /// Configures the library to use Redis cache for MediatR request responses.
    /// </summary>
    /// <param name="options">The output cache configuration options.</param>
    /// <param name="configure">Action used to configure Redis provider options.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if neither a connection string nor configuration options are provided.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a cache type has already been configured.</exception>
    public static void UseRedisCache(
        this RequestOutputCacheConfigurationOption options,
        Action<RedisRequestOutputCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        if (options.RequestOutputCacheType != default)
            throw new InvalidOperationException(ErrorMessages.AlreadyConfigured);

        var redisOptions = new RedisRequestOutputCacheOptions();
        configure(redisOptions);

        if (redisOptions.ConfigurationOptions is null && string.IsNullOrWhiteSpace(redisOptions.ConnectionString))
            throw new ArgumentException(ErrorMessages.EmptyConnectionString, nameof(configure));

        options.RequestOutputCacheType = RequestOutputCacheType.RedisCache;

        RequestOutputCacheDefaultsRegistration.Apply(options.Services, redisOptions.DefaultExpirationInSeconds);

        var multiplexer = new Lazy<IConnectionMultiplexer>(
            () => CreateMultiplexer(redisOptions),
            LazyThreadSafetyMode.ExecutionAndPublication);

        options.Services.TryAddSingleton(_ => multiplexer.Value);

        options.Services.AddStackExchangeRedisCache(cacheOptions =>
        {
            ApplyDistributedCacheOptions(
                cacheOptions,
                redisOptions.ConnectionString,
                redisOptions.InstanceName,
                redisOptions.Database,
                redisOptions.ConfigurationOptions);

            cacheOptions.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer.Value);
        });

        options.Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RedisRequestOutputCache<,>));
        options.Services.AddScoped<IRequestOutputCacheInvalidator, RedisRequestOutputCache<IRequest<object>, object>>();
        options.Services.AddScoped<IRequestOutputCacheContainer>(provider => new RedisOutputCacheContainer(
            provider.GetRequiredService<IDistributedCache>(),
            provider.GetRequiredService<IConnectionMultiplexer>(),
            provider.GetRequiredService<IOptions<Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions>>()));

        if (!redisOptions.EnableDistributedEviction)
            return;

        var channel = RequestOutputCacheEvictionChannel.Resolve(redisOptions.InstanceName, redisOptions.EvictionChannel);
        options.Services.TryAddSingleton(new RedisEvictionOptions { Channel = channel });
        options.Services.TryAddSingleton<IRequestOutputCacheEvictionNotifier, RedisRequestOutputCacheEvictionNotifier>();
        options.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, RedisRequestOutputCacheEvictionListener>());
    }

    internal static IConnectionMultiplexer CreateMultiplexer(RedisRequestOutputCacheOptions redisOptions)
    {
        if (redisOptions.ConfigurationOptions is not null)
        {
            var clone = redisOptions.ConfigurationOptions.Clone();
            if (redisOptions.Database.HasValue)
                clone.DefaultDatabase = redisOptions.Database;

            return ConnectionMultiplexer.Connect(clone);
        }

        if (redisOptions.Database.HasValue)
        {
            var parsed = ConfigurationOptions.Parse(redisOptions.ConnectionString!);
            parsed.DefaultDatabase = redisOptions.Database;
            return ConnectionMultiplexer.Connect(parsed);
        }

        return ConnectionMultiplexer.Connect(redisOptions.ConnectionString!);
    }

    internal static void ApplyDistributedCacheOptions(
        Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions cacheOptions,
        string? connectionString,
        string? instanceName,
        int? database,
        ConfigurationOptions? configurationOptions)
    {
        cacheOptions.InstanceName = instanceName;

        if (configurationOptions is not null)
        {
            if (database.HasValue)
                configurationOptions.DefaultDatabase = database;

            cacheOptions.ConfigurationOptions = configurationOptions;
            return;
        }

        if (database.HasValue)
        {
            var parsed = ConfigurationOptions.Parse(connectionString!);
            parsed.DefaultDatabase = database;
            cacheOptions.ConfigurationOptions = parsed;
            return;
        }

        cacheOptions.Configuration = connectionString;
    }
}
