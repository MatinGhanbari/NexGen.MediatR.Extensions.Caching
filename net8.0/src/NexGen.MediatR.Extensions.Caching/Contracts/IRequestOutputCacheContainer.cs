using FluentResults;
using System.Collections.ObjectModel;

namespace NexGen.MediatR.Extensions.Caching.Contracts;

/// <summary>
/// A static container that holds mapping information for cached request types and tags.
/// </summary>
public interface IRequestOutputCacheContainer
{
    Task<Type?> GetResponseTypeAsync<TRequest>(CancellationToken cancellationToken = default);
    Task<Result> UpdateContainerAsync<TRequest>(IEnumerable<string>? tags = null, string? cacheKey = null, Type responseType = null, CancellationToken cancellationToken = default);
    Task<ReadOnlyDictionary<string, HashSet<string>>> GetCacheTagsAsync(CancellationToken cancellationToken = default);
    Task<ReadOnlyDictionary<string, HashSet<string?>>> GetCacheTypesAsync(CancellationToken cancellationToken = default);
}