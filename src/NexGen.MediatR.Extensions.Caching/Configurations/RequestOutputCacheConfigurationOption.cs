using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Containers;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Provides configuration options for MediatR request output caching.
/// Allows selecting the caching mechanism and registering required services.
/// </summary>
public class RequestOutputCacheConfigurationOption
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheConfigurationOption"/> class.
    /// </summary>
    /// <param name="services">The service collection to which caching services will be added.</param>
    public RequestOutputCacheConfigurationOption(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// The service collection to which caching services will be added.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// The selected cache type.
    /// </summary>
    public RequestOutputCacheType RequestOutputCacheType;

    /// <summary>
    /// Configures the library to use in-memory caching for MediatR request responses.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a cache type has already been configured.
    /// </exception>
    public void UseMemoryCache()
    {
        if (RequestOutputCacheType != default)
            throw new InvalidOperationException(ErrorMessages.AlreadyConfigured);

        RequestOutputCacheType = RequestOutputCacheType.MemoryCache;

        Services.AddMemoryCache();
        Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RequestOutputCache<,>));
        Services.AddScoped<IRequestOutputCacheInvalidator, RequestOutputCache<IRequest<object>, object>>();
        Services.AddSingleton<IRequestOutputCacheContainer, RequestOutputCacheContainer>();
    }

    /// <summary>
    /// Clears all cached entries during application startup.
    /// </summary>
    public void ClearCacheOnStartup()
    {
        if (RequestOutputCacheType == default)
            throw new InvalidOperationException(ErrorMessages.CacheProviderNotConfigured);

        using var scope = Services.BuildServiceProvider().CreateScope();
        var cacheInvalidator = scope.ServiceProvider.GetRequiredService<IRequestOutputCacheInvalidator>();
        cacheInvalidator.FlushAll(CancellationToken.None).GetAwaiter().GetResult();
    }
}
