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
}
