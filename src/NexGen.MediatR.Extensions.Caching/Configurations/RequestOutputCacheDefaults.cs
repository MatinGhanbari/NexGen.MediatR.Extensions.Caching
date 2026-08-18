namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Library-wide defaults for MediatR request output caching.
/// Applied when a <see cref="Attributes.RequestOutputCacheAttribute"/> uses the library expiration constant.
/// </summary>
public sealed class RequestOutputCacheDefaults
{
    /// <summary>
    /// Optional default cache lifetime in seconds.
    /// When set, replaces <see cref="Constants.RequestCacheConstants.DefaultExpirationInSeconds"/>
    /// for attributes that omit an explicit expiration value (constructor default).
    /// </summary>
    public int? DefaultExpirationInSeconds { get; set; }

    /// <summary>
    /// When <see langword="true"/>, a cache hit during an ASP.NET Core HTTP request
    /// sets the <c>X-NexGen-Output-Cache: HIT</c> response header.
    /// Defaults to <see langword="true"/>. Disable with
    /// <see cref="RequestOutputCacheConfigurationOption.EnableCacheHitResponseHeader"/>.
    /// </summary>
    public bool EnableCacheHitResponseHeader { get; set; } = true;

    /// <summary>
    /// When <see langword="true"/>, unsuccessful handler responses are cached
    /// (FluentResults failures and types whose <c>IsSuccess</c> property is <see langword="false"/>).
    /// Defaults to <see langword="false"/>. A predicate registered with
    /// <see cref="RequestOutputCacheConfigurationOption.CacheWhen{TRequest, TResponse}(Func{TResponse, bool})"/>
    /// always takes priority. Enable with
    /// <see cref="RequestOutputCacheConfigurationOption.CacheUnsuccessfulResponses"/>.
    /// </summary>
    public bool CacheUnsuccessfulResponses { get; set; }
}
