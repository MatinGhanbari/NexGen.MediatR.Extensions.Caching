using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Behaviors;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

public static class RequestOutputCacheConfiguration
{
    public static IServiceCollection AddMediatorOutputCache(this IServiceCollection services, Action<RequestOutputCacheConfigurationOption> action)
    {
        var options = new RequestOutputCacheConfigurationOption(services);
        action.Invoke(options);

        return services.AddMediatorOutputCache();
    }

    private static IServiceCollection AddMediatorOutputCache(this IServiceCollection services)
    {
        return services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RequestOutputCacheBehavior<,>));
    }
}