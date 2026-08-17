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
    /// Auto-evict and <c>[RequestOutputCacheEvict]</c> apply only in this process.
    /// Cross-service invalidation is not supported with the memory provider.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a cache type has already been configured.
    /// </exception>
    public void UseMemoryCache()
    {
        UseMemoryCache(_ => { });
    }

    /// <summary>
    /// Configures the library to use in-memory caching for MediatR request responses.
    /// Auto-evict and <c>[RequestOutputCacheEvict]</c> apply only in this process.
    /// Cross-service invalidation is not supported with the memory provider.
    /// </summary>
    /// <param name="configure">Action used to configure memory cache provider options.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configure"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if a cache type has already been configured.
    /// </exception>
    public void UseMemoryCache(Action<MemoryRequestOutputCacheOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);

        if (RequestOutputCacheType != default)
            throw new InvalidOperationException(ErrorMessages.AlreadyConfigured);

        var memoryOptions = new MemoryRequestOutputCacheOptions();
        configure(memoryOptions);

        RequestOutputCacheType = RequestOutputCacheType.MemoryCache;

        RequestOutputCacheDefaultsRegistration.Apply(Services, memoryOptions.DefaultExpirationInSeconds);

        Services.AddMemoryCache();
        Services.AddScoped(typeof(IRequestOutputCache<,>), typeof(RequestOutputCache<,>));
        Services.AddScoped<IRequestOutputCacheInvalidator, RequestOutputCache<IRequest<object>, object>>();
        Services.AddSingleton<IRequestOutputCacheContainer, RequestOutputCacheContainer>();
    }

    /// <summary>
    /// Controls whether a cache hit during an ASP.NET Core HTTP request sets
    /// the <c>X-NexGen-Output-Cache: HIT</c> response header.
    /// Enabled by default. Pass <see langword="false"/> to disable.
    /// Non-HTTP MediatR executions are unchanged.
    /// </summary>
    /// <param name="enabled"><see langword="true"/> to set the header on cache hits; <see langword="false"/> to skip it.</param>
    public void EnableCacheHitResponseHeader(bool enabled)
    {
        RequestOutputCacheDefaultsRegistration.Apply(Services, defaultExpirationInSeconds: null, enableCacheHitResponseHeader: enabled);
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
