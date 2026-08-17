using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Registers <see cref="RequestOutputCacheDefaults"/> during provider configuration.
/// </summary>
public static class RequestOutputCacheDefaultsRegistration
{
    /// <summary>
    /// Ensures a <see cref="RequestOutputCacheDefaults"/> singleton is available, and applies
    /// <paramref name="defaultExpirationInSeconds"/> when provided.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultExpirationInSeconds">Optional provider-level default expiration.</param>
    public static void Apply(IServiceCollection services, int? defaultExpirationInSeconds)
        => Apply(services, defaultExpirationInSeconds, enableCacheHitResponseHeader: null);

    /// <summary>
    /// Ensures a <see cref="RequestOutputCacheDefaults"/> singleton is available, and applies
    /// provider defaults when provided.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="defaultExpirationInSeconds">Optional provider-level default expiration.</param>
    /// <param name="enableCacheHitResponseHeader">
    /// Optional override for <see cref="RequestOutputCacheDefaults.EnableCacheHitResponseHeader"/>.
    /// </param>
    public static void Apply(
        IServiceCollection services,
        int? defaultExpirationInSeconds,
        bool? enableCacheHitResponseHeader)
    {
        ArgumentNullException.ThrowIfNull(services);

        var existing = services.FirstOrDefault(d => d.ServiceType == typeof(RequestOutputCacheDefaults));
        if (existing?.ImplementationInstance is RequestOutputCacheDefaults instance)
        {
            if (defaultExpirationInSeconds.HasValue)
                instance.DefaultExpirationInSeconds = defaultExpirationInSeconds;
            if (enableCacheHitResponseHeader.HasValue)
                instance.EnableCacheHitResponseHeader = enableCacheHitResponseHeader.Value;
            return;
        }

        services.TryAddSingleton(new RequestOutputCacheDefaults
        {
            DefaultExpirationInSeconds = defaultExpirationInSeconds,
            EnableCacheHitResponseHeader = enableCacheHitResponseHeader ?? true
        });
    }
}
