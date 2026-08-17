using FluentResults;
using System.Collections.ObjectModel;

namespace NexGen.MediatR.Extensions.Caching.Contracts;

/// <summary>
/// A static container that holds mapping information for cached request types and tags.
/// </summary>
public interface IRequestOutputCacheContainer
{
    Task<Type?> GetResponseTypeAsync<TRequest>(CancellationToken cancellationToken = default);
    Task<Result> UpdateContainerAsync<TRequest>(IEnumerable<string>? tags = null, string? cacheKey = null, Type? responseType = null, CancellationToken cancellationToken = default);
    Task<ReadOnlyDictionary<string, HashSet<string>>> GetCacheTagsAsync(CancellationToken cancellationToken = default);
    Task<ReadOnlyDictionary<string, HashSet<string?>>> GetCacheTypesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes index metadata for the specified request types after their cache entries are evicted.
    /// </summary>
    /// <param name="requestTypeNames">The full names of the request types to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful result when the index metadata has been removed.</returns>
    Task<Result> RemoveRequestTypesAsync(
        IEnumerable<string> requestTypeNames,
        CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Ok());

    /// <summary>
    /// Clears all cache index metadata.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A successful result when the index metadata has been cleared.</returns>
    Task<Result> ClearAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result.Ok());
}