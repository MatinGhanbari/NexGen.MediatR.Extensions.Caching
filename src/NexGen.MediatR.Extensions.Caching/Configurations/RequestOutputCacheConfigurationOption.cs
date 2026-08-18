using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
    /// Caches unsuccessful handler responses (FluentResults failures and types whose
    /// <c>IsSuccess</c> property is <see langword="false"/>), restoring pre-2.3 behavior.
    /// Defaults to <see langword="false"/>. A predicate registered with
    /// <see cref="CacheWhen{TRequest, TResponse}(Func{TResponse, bool})"/> always takes priority.
    /// </summary>
    /// <param name="enabled">
    /// <see langword="true"/> to cache every non-null response when no custom predicate is registered.
    /// </param>
    public void CacheUnsuccessfulResponses(bool enabled)
    {
        RequestOutputCacheDefaultsRegistration.Apply(
            Services,
            defaultExpirationInSeconds: null,
            enableCacheHitResponseHeader: null,
            cacheUnsuccessfulResponses: enabled);
    }

    /// <summary>
    /// Caches the handler response only when <paramref name="predicate"/> returns <see langword="true"/>.
    /// The predicate receives the whole response (for FluentResults, that is the <c>Result</c> /
    /// <c>Result&lt;T&gt;</c> instance). Exceptions thrown by the predicate are not caught.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type.</typeparam>
    /// <typeparam name="TResponse">The MediatR response type.</typeparam>
    /// <param name="predicate">Returns <see langword="true"/> when the response should be cached.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public void CacheWhen<TRequest, TResponse>(Func<TResponse, bool> predicate)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(predicate);
        CacheWhen<TRequest, TResponse>((_, response) => predicate(response));
    }

    /// <summary>
    /// Caches the handler response only when <paramref name="predicate"/> returns <see langword="true"/>.
    /// The predicate receives both the request and the whole response. Exceptions thrown by the
    /// predicate are not caught.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type.</typeparam>
    /// <typeparam name="TResponse">The MediatR response type.</typeparam>
    /// <param name="predicate">Returns <see langword="true"/> when the response should be cached.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public void CacheWhen<TRequest, TResponse>(Func<TRequest, TResponse, bool> predicate)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(predicate);
        GetOrAddConditions().Register(predicate);
    }

    private RequestOutputCacheConditions GetOrAddConditions()
    {
        var existing = Services.FirstOrDefault(d => d.ServiceType == typeof(RequestOutputCacheConditions));
        if (existing?.ImplementationInstance is RequestOutputCacheConditions instance)
            return instance;

        var conditions = new RequestOutputCacheConditions();
        Services.TryAddSingleton(conditions);
        return conditions;
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
