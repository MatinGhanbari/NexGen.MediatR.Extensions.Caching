using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Attributes;

/// <summary>
/// Marks a MediatR request so that after a successful handler execution the listed cache tags are
/// published on the eviction bus (or evicted locally when no publisher is registered).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequestOutputCacheEvictAttribute : Attribute
{
    /// <summary>
    /// Gets the cache tags to invalidate.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheEvictAttribute"/> class.
    /// </summary>
    /// <param name="tags">Tags to publish or evict after the request succeeds.</param>
    public RequestOutputCacheEvictAttribute(params string[] tags)
    {
        Tags = tags ?? throw new ArgumentNullException(nameof(tags));
    }
}
