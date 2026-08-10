using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Shared helpers for registering eviction bus publishers and subscribers.
/// </summary>
internal static class RequestOutputCacheEvictionRegistration
{
    internal static void RegisterPublisher<TPublisher>(IServiceCollection services)
        where TPublisher : class, IRequestOutputCacheEvictionPublisher
    {
        services.TryAddSingleton<IRequestOutputCacheEvictionPublisher, TPublisher>();
    }

    internal static void RegisterPublisher(
        IServiceCollection services,
        IRequestOutputCacheEvictionPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        services.TryAddSingleton(publisher);
        services.TryAddSingleton<IRequestOutputCacheEvictionPublisher>(publisher);
    }

    internal static void RegisterSubscriber<TSubscriber>(IServiceCollection services, bool startHostedService)
        where TSubscriber : class, IRequestOutputCacheEvictionSubscriber
    {
        services.TryAddSingleton<IRequestOutputCacheEvictionSubscriber, TSubscriber>();
        if (startHostedService)
            TryAddHostedService(services);
    }

    internal static void RegisterSubscriber(
        IServiceCollection services,
        IRequestOutputCacheEvictionSubscriber subscriber,
        bool startHostedService)
    {
        ArgumentNullException.ThrowIfNull(subscriber);
        services.TryAddSingleton(subscriber);
        services.TryAddSingleton<IRequestOutputCacheEvictionSubscriber>(subscriber);
        if (startHostedService)
            TryAddHostedService(services);
    }

    internal static void RegisterInProcessBus(
        IServiceCollection services,
        InProcessRequestOutputCacheEvictionBus bus,
        bool subscribe)
    {
        ArgumentNullException.ThrowIfNull(bus);

        services.TryAddSingleton(bus);
        services.TryAddSingleton<IRequestOutputCacheEvictionPublisher>(bus);

        if (!subscribe)
            return;

        services.TryAddSingleton<IRequestOutputCacheEvictionSubscriber>(bus);
        TryAddHostedService(services);
    }

    internal static void TryAddHostedService(IServiceCollection services)
    {
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<Microsoft.Extensions.Hosting.IHostedService, RequestOutputCacheEvictionHostedService>());
    }
}
