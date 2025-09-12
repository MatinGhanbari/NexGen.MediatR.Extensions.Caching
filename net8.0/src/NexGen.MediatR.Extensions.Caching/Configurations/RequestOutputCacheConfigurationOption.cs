using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Provides configuration options for MediatR request output caching.
/// Allows selecting the caching mechanism and registering required services.
/// </summary>
public class RequestOutputCacheConfigurationOption(IServiceCollection services)
{
    /// <summary>
    /// The service collection to which caching services will be added.
    /// </summary>
    public readonly IServiceCollection Services = services;

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
            throw new Exception("MediatR Response Cache already added.");

        if (Services == null) throw new ArgumentNullException(nameof(Services));

        RequestOutputCacheType = RequestOutputCacheType.MemoryCache;

        Services.AddMemoryCache();
        Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RequestOutputCache<,>));
    }
}
