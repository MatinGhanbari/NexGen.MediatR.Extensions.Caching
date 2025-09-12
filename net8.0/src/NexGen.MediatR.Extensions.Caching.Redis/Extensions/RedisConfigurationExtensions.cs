using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;

namespace NexGen.MediatR.Extensions.Caching.Redis.Extensions;

public static class RedisConfigurationExtensions
{
    public static void UseRedisCache(this RequestOutputCacheConfigurationOption options, string connectionString)
    {
        if (options == null) 
            throw new ArgumentNullException(nameof(options));

        if (options.RequestOutputCacheType != default)
            throw new InvalidOperationException(ErrorMessages.AlreadyConfigured);

        if (string.IsNullOrWhiteSpace(connectionString)) 
            throw new ArgumentException(ErrorMessages.EmptyConnectionString, nameof(connectionString));

        options.RequestOutputCacheType = RequestOutputCacheType.RedisCache;

        options.Services.AddStackExchangeRedisCache(redisOptions =>
        {
            redisOptions.Configuration = connectionString;
        });

        options.Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RedisRequestOutputCache<,>));
    }
}