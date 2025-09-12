using NexGen.MediatR.Extensions.Caching.Constants;

namespace NexGen.MediatR.Extensions.Caching.Attributes;

/// <summary>
/// Specifies that the response of a MediatR request should be cached.
/// Apply this attribute to a request handler class to enable automatic output caching.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class RequestOutputCacheAttribute : Attribute
{
    /// <summary>
    /// Gets the cache tags associated with this request.
    /// Tags can be used to group related cache entries and support cache invalidation.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Gets the expiration time of the cache entry, in seconds.
    /// If set to <c>0</c>, the cache entry never expires.
    /// Defaults to <see cref="RequestCacheConstants.ExpirationInSeconds"/>.
    /// </summary>
    public int ExpirationInSeconds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheAttribute"/> class.
    /// </summary>
    /// <param name="tags">
    /// An array of tags to associate with the cache entry. Used for grouping and invalidation.
    /// </param>
    /// <param name="expirationInSeconds">
    /// The cache lifetime in seconds. Optional. Set it Zero to never expire cache. Defaults to <see cref="RequestCacheConstants.ExpirationInSeconds"/>.
    /// If set to <c>0</c>, the cache entry never expires.
    /// </param>
    public RequestOutputCacheAttribute(string[] tags, int expirationInSeconds = RequestCacheConstants.ExpirationInSeconds)
    {
        Tags = tags;
        ExpirationInSeconds = expirationInSeconds;
    }
}