using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Behaviors;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Provides extension methods to configure MediatR request output caching.
/// </summary>
public static class RequestOutputCacheConfiguration
{
    
    /// <summary>
    /// Adds MediatR output caching services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The service collection to add caching to.</param>
    /// <param name="action">
    /// An action to configure <see cref="IServiceCollection"/> 
    /// (e.g., registering a cache provider or configuring options).
    /// </param>
    /// <returns>The updated <see cref="RequestOutputCacheConfigurationOption"/>.</returns>
    public static IServiceCollection AddMediatROutputCache(this IServiceCollection services, Action<RequestOutputCacheConfigurationOption> action)
    {
        var options = new RequestOutputCacheConfigurationOption(services);
        action.Invoke(options);

        return services.AddMediatROutputCache();
    }

    /// <summary>
    /// Registers the internal MediatR pipeline behavior for request output caching.
    /// </summary>
    /// <param name="services">The service collection to register the behavior with.</param>
    /// <returns>The updated <see cref="IServiceCollection"/>.</returns>
    private static IServiceCollection AddMediatROutputCache(this IServiceCollection services)
    {
        return services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RequestOutputCacheBehavior<,>));
    }
}