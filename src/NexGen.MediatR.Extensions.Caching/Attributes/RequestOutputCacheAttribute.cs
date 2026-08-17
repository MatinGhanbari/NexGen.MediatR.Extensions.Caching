using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.Attributes;

/// <summary>
/// Marks a MediatR request so its handler response is cached.
/// Apply this attribute to the request type (the class that implements <c>IRequest&lt;TResponse&gt;</c>).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public class RequestOutputCacheAttribute : Attribute
{
    /// <summary>
    /// Gets the cache tags associated with this request.
    /// Tags group related cache entries and are used for invalidation.
    /// Empty and duplicate values are removed; remaining tags are trimmed.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Gets the expiration time of the cache entry, in seconds.
    /// If set to <c>0</c>, the cache entry never expires.
    /// Defaults to <see cref="RequestCacheConstants.DefaultExpirationInSeconds"/>.
    /// </summary>
    public int ExpirationInSeconds { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheAttribute"/> class.
    /// </summary>
    /// <param name="tags">
    /// Tags to associate with the cache entry. Used for grouping and invalidation.
    /// </param>
    /// <param name="expirationInSeconds">
    /// The cache lifetime in seconds. Optional. Set it Zero to never expire cache. Defaults to <see cref="RequestCacheConstants.DefaultExpirationInSeconds"/>.
    /// If set to <c>0</c>, the cache entry never expires.
    /// </param>
    public RequestOutputCacheAttribute(string[] tags, int expirationInSeconds = RequestCacheConstants.DefaultExpirationInSeconds)
    {
        Tags = RequestOutputCacheTagNormalizer.Normalize(tags);
        ExpirationInSeconds = expirationInSeconds;
    }
}
