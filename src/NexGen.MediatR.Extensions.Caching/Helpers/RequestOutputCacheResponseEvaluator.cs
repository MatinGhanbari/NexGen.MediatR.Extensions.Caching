using System.Collections.Concurrent;
using System.Reflection;
using FluentResults;

namespace NexGen.MediatR.Extensions.Caching.Helpers;

/// <summary>
/// Decides whether a handler response should be cached when no custom
/// <see cref="Configurations.RequestOutputCacheConditions"/> predicate is registered.
/// </summary>
internal static class RequestOutputCacheResponseEvaluator
{
    private const string IsSuccessPropertyName = "IsSuccess";

    private static readonly ConcurrentDictionary<Type, PropertyInfo?> IsSuccessProperties = new();

    /// <summary>
    /// Returns <see langword="true"/> when <paramref name="response"/> should be stored.
    /// Null responses are never cached. When <paramref name="cacheUnsuccessfulResponses"/> is
    /// <see langword="true"/>, every non-null response is cached. Otherwise FluentResults
    /// <see cref="IResultBase.IsSuccess"/> is used when applicable; if the response is not a
    /// FluentResults type, a public <c>IsSuccess</c> <see cref="bool"/> property is used when present;
    /// otherwise the response is cached.
    /// </summary>
    /// <typeparam name="TResponse">The handler response type.</typeparam>
    /// <param name="response">The handler response.</param>
    /// <param name="cacheUnsuccessfulResponses">
    /// When <see langword="true"/>, skip success checks and cache every non-null response.
    /// </param>
    /// <returns><see langword="true"/> to cache; <see langword="false"/> to skip.</returns>
    internal static bool ShouldCache<TResponse>(TResponse response, bool cacheUnsuccessfulResponses)
    {
        if (response is null)
            return false;

        if (cacheUnsuccessfulResponses)
            return true;

        if (response is IResultBase result)
            return result.IsSuccess;

        var property = IsSuccessProperties.GetOrAdd(response.GetType(), static type =>
        {
            var candidate = type.GetProperty(IsSuccessPropertyName, BindingFlags.Instance | BindingFlags.Public);
            if (candidate is null || candidate.PropertyType != typeof(bool) || !candidate.CanRead)
                return null;

            return candidate;
        });

        if (property is null)
            return true;

        return (bool)property.GetValue(response)!;
    }
}
