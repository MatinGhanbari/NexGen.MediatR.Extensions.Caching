using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using NexGen.MediatR.Extensions.Caching.Enums;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Registers startup validation for MediatR output cache DI options.
/// </summary>
internal static class RequestOutputCacheOptionsValidationRegistration
{
    internal static void Register(IServiceCollection services)
    {
        services.AddOptions<RequestOutputCacheRegistrationOptions>()
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<RequestOutputCacheRegistrationOptions>, RequestOutputCacheRegistrationOptionsValidator>());
    }

    internal static void SetCacheProvider(IServiceCollection services, RequestOutputCacheType cacheType)
    {
        services.Configure<RequestOutputCacheRegistrationOptions>(options => options.CacheType = cacheType);
    }

    internal static void RegisterMemoryProviderOptions(
        IServiceCollection services,
        MemoryRequestOutputCacheOptions options)
    {
        services.AddOptions<MemoryRequestOutputCacheOptions>()
            .Configure(config =>
            {
                config.DefaultExpirationInSeconds = options.DefaultExpirationInSeconds;
            })
            .ValidateOnStart();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<MemoryRequestOutputCacheOptions>, MemoryRequestOutputCacheOptionsValidator>());
    }
}
