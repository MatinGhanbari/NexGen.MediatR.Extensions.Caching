using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;

namespace NexGen.MediatR.Extensions.Caching.Redis.Extensions;

public static class RequestOutputCacheConfigurationOptionExtensions
{
    public static void UseRedisCache(this RequestOutputCacheConfigurationOption options, string connectionString)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (options._cacheType != default)
            throw new Exception("MediatR Response Cache already added.");

        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        options._cacheType = CacheType.RedisCache;

        options.Services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = connectionString;
        });

        options.Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RedisRequestOutputCache<,>));
    }
}