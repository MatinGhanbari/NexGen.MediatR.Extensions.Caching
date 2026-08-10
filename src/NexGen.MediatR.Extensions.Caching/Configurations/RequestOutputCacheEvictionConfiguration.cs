using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Behaviors;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Extension methods for registering command-side output-cache eviction (publisher) without the query cache pipeline.
/// </summary>
public static class RequestOutputCacheEvictionConfiguration
{
    /// <summary>
    /// Adds MediatR output-cache eviction publishing for CQRS command hosts that do not serve cached queries.
    /// Registers <see cref="RequestOutputCacheEvictBehavior{TRequest,TResponse}"/> for
    /// <see cref="Attributes.RequestOutputCacheEvictAttribute"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Configures the eviction bus publisher.</param>
    /// <returns>The same service collection.</returns>
    public static IServiceCollection AddMediatROutputCacheEviction(
        this IServiceCollection services,
        Action<RequestOutputCacheEvictionConfigurationOption> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        var options = new RequestOutputCacheEvictionConfigurationOption(services);
        configure(options);

        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(RequestOutputCacheEvictBehavior<,>));
        return services;
    }
}
