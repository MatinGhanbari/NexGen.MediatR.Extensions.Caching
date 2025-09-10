using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

public class RequestOutputCacheConfigurationOption(IServiceCollection services)
{
    public readonly IServiceCollection Services = services;
    public CacheType _cacheType;

    public void UseMemoryCache()
    {
        if (_cacheType != default)
            throw new Exception("MediatR Response Cache already added.");

        if (Services == null) throw new ArgumentNullException(nameof(Services));

        _cacheType = CacheType.MemoryCache;

        Services.AddMemoryCache();
        Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RequestOutputCache<,>));
    }
}
