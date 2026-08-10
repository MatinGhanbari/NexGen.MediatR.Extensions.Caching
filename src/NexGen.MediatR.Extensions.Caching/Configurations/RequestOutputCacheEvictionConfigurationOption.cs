using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Configuration options for command-side (publisher-only) MediatR output-cache eviction.
/// </summary>
public sealed class RequestOutputCacheEvictionConfigurationOption
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheEvictionConfigurationOption"/> class.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    public RequestOutputCacheEvictionConfigurationOption(IServiceCollection services)
    {
        Services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// The service collection being configured.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    /// Registers a shared in-process eviction bus as the publisher (no subscriber).
    /// Use the same instance on the query host with <see cref="RequestOutputCacheConfigurationOption.UseInProcessEvictionBus"/>.
    /// </summary>
    /// <param name="bus">Shared bus instance.</param>
    public void UseInProcessEvictionBus(InProcessRequestOutputCacheEvictionBus bus)
    {
        RequestOutputCacheEvictionRegistration.RegisterInProcessBus(Services, bus, subscribe: false);
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
}
