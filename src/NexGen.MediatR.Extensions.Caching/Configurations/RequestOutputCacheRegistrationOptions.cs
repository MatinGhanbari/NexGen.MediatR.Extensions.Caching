namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Captures MediatR output cache registration choices for startup validation.
/// </summary>
internal sealed class RequestOutputCacheRegistrationOptions
{
    /// <summary>
    /// The cache provider selected during DI configuration.
    /// </summary>
    public Enums.RequestOutputCacheType CacheType { get; set; }
}
