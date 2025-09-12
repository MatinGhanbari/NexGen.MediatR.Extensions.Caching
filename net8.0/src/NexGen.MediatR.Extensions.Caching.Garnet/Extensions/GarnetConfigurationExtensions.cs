using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Extensions;

public static class GarnetConfigurationExtensions
{
    public static void UseGarnetCache(this RequestOutputCacheConfigurationOption options, string connectionString)
    {
        if (options == null) throw new ArgumentNullException(nameof(options));

        if (options.RequestOutputCacheType != default)
            throw new Exception("MediatR Response Cache already added.");

        if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Connection string cannot be empty.", nameof(connectionString));

        options.RequestOutputCacheType = RequestOutputCacheType.GarnetCache;

        options.Services.AddStackExchangeRedisCache(garnetCacheOptions =>
        {
            garnetCacheOptions.Configuration = connectionString;
        });

        options.Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(GarnetRequestOutputCache<,>));
    }
}