using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;
using NexGen.MediatR.Extensions.Caching.Garnet.Containers;
using NexGen.MediatR.Extensions.Caching.Garnet.Eviction;
using NexGen.MediatR.Extensions.Caching.Helpers;
using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Configurations;

/// <summary>
/// Provides extension methods to configure Garnet caching for MediatR requests.
/// </summary>
public static class GarnetConfigurationExtensions
{
    /// <summary>
    /// Configures the library to use Garnet cache for MediatR request responses.
    /// Distributed tag eviction over Garnet Pub/Sub is enabled by default.
    /// </summary>
    /// <param name="options">The output cache configuration options.</param>
    /// <param name="connectionString">The connection string for the Garnet cache.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="connectionString"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a cache type has already been configured.</exception>
    public static void UseGarnetCache(this RequestOutputCacheConfigurationOption options, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(options);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(ErrorMessages.EmptyConnectionString, nameof(connectionString));

        options.UseGarnetCache(garnet => garnet.ConnectionString = connectionString);
    }

    /// <summary>
    /// Configures the library to use Garnet cache for MediatR request responses.
    /// </summary>
    /// <param name="options">The output cache configuration options.</param>
    /// <param name="configure">Action used to configure Garnet provider options.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if neither a connection string nor configuration options are provided.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a cache type has already been configured.</exception>
    public static void UseGarnetCache(
        this RequestOutputCacheConfigurationOption options,
        Action<GarnetRequestOutputCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        if (options.RequestOutputCacheType != default)
            throw new InvalidOperationException(ErrorMessages.AlreadyConfigured);

        var garnetOptions = new GarnetRequestOutputCacheOptions();
        configure(garnetOptions);

        if (garnetOptions.ConfigurationOptions is null && string.IsNullOrWhiteSpace(garnetOptions.ConnectionString))
            throw new ArgumentException(ErrorMessages.EmptyConnectionString, nameof(configure));

        options.RequestOutputCacheType = RequestOutputCacheType.GarnetCache;

        RequestOutputCacheDefaultsRegistration.Apply(options.Services, garnetOptions.DefaultExpirationInSeconds);

        var multiplexer = new Lazy<IConnectionMultiplexer>(
            () => CreateMultiplexer(garnetOptions),
            LazyThreadSafetyMode.ExecutionAndPublication);

        options.Services.TryAddSingleton(_ => multiplexer.Value);

        options.Services.AddStackExchangeRedisCache(cacheOptions =>
        {
            ApplyDistributedCacheOptions(
                cacheOptions,
                garnetOptions.ConnectionString,
                garnetOptions.InstanceName,
                garnetOptions.Database,
                garnetOptions.ConfigurationOptions);

            cacheOptions.ConnectionMultiplexerFactory = () => Task.FromResult(multiplexer.Value);
        });

        options.Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(GarnetRequestOutputCache<,>));
        options.Services.AddScoped<IRequestOutputCacheInvalidator, GarnetRequestOutputCache<IRequest<object>, object>>();
        options.Services.AddScoped<IRequestOutputCacheContainer, GarnetOutputCacheContainer>();

        if (!garnetOptions.EnableDistributedEviction)
            return;

        var channel = RequestOutputCacheEvictionChannel.Resolve(garnetOptions.InstanceName, garnetOptions.EvictionChannel);
        options.Services.TryAddSingleton(new GarnetEvictionOptions { Channel = channel });
        options.Services.TryAddSingleton<IRequestOutputCacheEvictionNotifier, GarnetRequestOutputCacheEvictionNotifier>();
        options.Services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, GarnetRequestOutputCacheEvictionListener>());
    }

    internal static IConnectionMultiplexer CreateMultiplexer(GarnetRequestOutputCacheOptions garnetOptions)
    {
        if (garnetOptions.ConfigurationOptions is not null)
        {
            var clone = garnetOptions.ConfigurationOptions.Clone();
            if (garnetOptions.Database.HasValue)
                clone.DefaultDatabase = garnetOptions.Database;

            return ConnectionMultiplexer.Connect(clone);
        }

        if (garnetOptions.Database.HasValue)
        {
            var parsed = ConfigurationOptions.Parse(garnetOptions.ConnectionString!);
            parsed.DefaultDatabase = garnetOptions.Database;
            return ConnectionMultiplexer.Connect(parsed);
        }

        return ConnectionMultiplexer.Connect(garnetOptions.ConnectionString!);
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
