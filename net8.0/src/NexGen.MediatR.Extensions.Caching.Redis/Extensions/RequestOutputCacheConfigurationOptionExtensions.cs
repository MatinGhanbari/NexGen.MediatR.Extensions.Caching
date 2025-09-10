using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Redis.Extensions;

public static class RequestOutputCacheConfigurationOptionExtensions
{
    public static void UseRedisCache(this RequestOutputCacheConfigurationOption options, string connectionString)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));
        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        options.Services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = connectionString;
        });

        options.Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RedisRequestOutputCache<,>));
    }
}