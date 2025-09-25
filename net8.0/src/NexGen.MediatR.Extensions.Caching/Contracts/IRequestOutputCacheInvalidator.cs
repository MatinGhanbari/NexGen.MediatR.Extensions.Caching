using FluentResults;

namespace NexGen.MediatR.Extensions.Caching.Contracts;

/// <summary>
/// Defines a contract for invalidate caching of MediatR requests.
/// </summary>
public interface IRequestOutputCacheInvalidator
{
    /// <summary>
    /// Evicts cached entries associated with the specified tags.
    /// </summary>
    /// <param name="tags">The tags whose associated cache entries should be evicted.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A <see cref="Result"/> indicating success or failure of the eviction.</returns>
    Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}