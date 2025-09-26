using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;
using NexGen.MediatR.Extensions.Caching.Redis.Containers;

namespace NexGen.MediatR.Extensions.Caching.Redis.Configurations;

/// <summary>
/// Provides extension methods to configure Redis caching for MediatR requests.
/// </summary>
public static class RedisConfigurationExtensions
{
    /// <summary>
    /// Configures the library to use Redis cache for MediatR request responses.
    /// </summary>
    /// <param name="options">The output cache configuration options.</param>
    /// <param name="connectionString">The connection string for the Redis server.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="connectionString"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">Thrown if a cache type has already been configured.</exception>
    public static void UseRedisCache(this RequestOutputCacheConfigurationOption options, string connectionString)
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (options.RequestOutputCacheType != default)
            throw new InvalidOperationException(ErrorMessages.AlreadyConfigured);

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException(ErrorMessages.EmptyConnectionString, nameof(connectionString));

        options.RequestOutputCacheType = RequestOutputCacheType.RedisCache;

        // Configure StackExchange.Redis cache
        options.Services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = connectionString;
        });

        // Register the Redis request output cache implementation
        options.Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RedisRequestOutputCache<,>));
        options.Services.AddScoped<IRequestOutputCacheInvalidator, RedisRequestOutputCache<IRequest<object>, object>>();
        options.Services.AddScoped<IRequestOutputCacheContainer, RedisOutputCacheContainer>();
    }
}