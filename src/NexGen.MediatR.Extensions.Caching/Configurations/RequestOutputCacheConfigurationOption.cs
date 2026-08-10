using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Containers;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Enums;
using NexGen.MediatR.Extensions.Caching.Eviction;

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
    /// Registers a shared in-process eviction bus as publisher and subscriber, and starts the eviction hosted service.
    /// Use the same instance on the command host with <see cref="RequestOutputCacheEvictionConfigurationOption.UseInProcessEvictionBus"/>.
    /// </summary>
    /// <param name="bus">Shared bus instance.</param>
    public void UseInProcessEvictionBus(InProcessRequestOutputCacheEvictionBus bus)
    {
        RequestOutputCacheEvictionRegistration.RegisterInProcessBus(Services, bus, subscribe: true);
    }

    /// <summary>
    /// Registers a custom eviction publisher (for example a RabbitMQ or Kafka adapter).
    /// </summary>
    /// <typeparam name="TPublisher">Publisher implementation type.</typeparam>
    public void UseCustomEvictionPublisher<TPublisher>()
        where TPublisher : class, IRequestOutputCacheEvictionPublisher
    {
        RequestOutputCacheEvictionRegistration.RegisterPublisher<TPublisher>(Services);
    }

    /// <summary>
    /// Registers an existing publisher instance.
    /// </summary>
    /// <param name="publisher">Publisher instance.</param>
    public void UseCustomEvictionPublisher(IRequestOutputCacheEvictionPublisher publisher)
    {
        RequestOutputCacheEvictionRegistration.RegisterPublisher(Services, publisher);
    }

    /// <summary>
    /// Registers a custom eviction subscriber and starts the hosted service that applies
    /// received messages via <see cref="IRequestOutputCacheInvalidator"/>.
    /// </summary>
    /// <typeparam name="TSubscriber">Subscriber implementation type.</typeparam>
    public void UseCustomEvictionSubscriber<TSubscriber>()
        where TSubscriber : class, IRequestOutputCacheEvictionSubscriber
    {
        RequestOutputCacheEvictionRegistration.RegisterSubscriber<TSubscriber>(Services, startHostedService: true);
    }

    /// <summary>
    /// Registers an existing subscriber instance and starts the eviction hosted service.
    /// </summary>
    /// <param name="subscriber">Subscriber instance.</param>
    public void UseCustomEvictionSubscriber(IRequestOutputCacheEvictionSubscriber subscriber)
    {
        RequestOutputCacheEvictionRegistration.RegisterSubscriber(Services, subscriber, startHostedService: true);
    }

    /// <summary>
    /// Registers both a custom publisher and subscriber and starts the eviction hosted service.
    /// </summary>
    /// <typeparam name="TPublisher">Publisher implementation type.</typeparam>
    /// <typeparam name="TSubscriber">Subscriber implementation type.</typeparam>
    public void UseCustomEvictionBus<TPublisher, TSubscriber>()
        where TPublisher : class, IRequestOutputCacheEvictionPublisher
        where TSubscriber : class, IRequestOutputCacheEvictionSubscriber
    {
        UseCustomEvictionPublisher<TPublisher>();
        UseCustomEvictionSubscriber<TSubscriber>();
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
