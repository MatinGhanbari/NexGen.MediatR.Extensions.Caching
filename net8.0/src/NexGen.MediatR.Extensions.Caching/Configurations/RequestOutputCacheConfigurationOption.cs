using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

public class RequestOutputCacheConfigurationOption(IServiceCollection services)
{
    public readonly IServiceCollection Services = services;

    public void UseMemoryCache()
    {
        if (Services == null) throw new ArgumentNullException(nameof(Services));

        Services.AddMemoryCache();
        Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RequestOutputCache<,>));
    }
}
