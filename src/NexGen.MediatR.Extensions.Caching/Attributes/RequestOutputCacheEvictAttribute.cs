using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.Attributes;

/// <summary>
/// Marks a MediatR request so that after a successful handler execution the listed cache tags are
/// invalidated in the current process and, when Redis or Garnet distributed eviction is enabled,
/// on other hosts via Pub/Sub. Multiple tags are sent in a single eviction.
/// </summary>
/// <remarks>
/// Eviction is skipped when the handler throws or returns a failed FluentResults <c>IResultBase</c>.
/// Memory cache invalidation stays in-process; it is not broadcast to other services.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class RequestOutputCacheEvictAttribute : Attribute
{
    /// <summary>
    /// Gets the cache tags to invalidate after a successful handler.
    /// Empty and duplicate values are removed; remaining tags are trimmed.
    /// </summary>
    public string[] Tags { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheEvictAttribute"/> class.
    /// </summary>
    /// <param name="tags">One or more tags to invalidate after the request succeeds.</param>
    public RequestOutputCacheEvictAttribute(params string[] tags)
    {
        Tags = RequestOutputCacheTagNormalizer.Normalize(tags);
    }
}
