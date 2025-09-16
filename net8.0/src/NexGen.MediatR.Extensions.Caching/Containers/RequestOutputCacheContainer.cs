using FluentResults;
using NexGen.MediatR.Extensions.Caching.Constants;

namespace NexGen.MediatR.Extensions.Caching.Containers;

/// <summary>
/// A static container that holds mapping information for cached request types and tags.
/// </summary>
public static class RequestOutputCacheContainer
{
    /// <summary>
    /// Maps request types to a set of associated cache tags.
    /// Key: request type (<see cref="Type"/>)
    /// Value: set of tags (<see cref="HashSet{String}"/>)
    /// </summary>
    public static Dictionary<Type, HashSet<string?>> CacheTypes = [];

    /// <summary>
    /// Maps cache tags to a set of request types that use them.
    /// Key: cache tag (<see cref="string"/>)
    /// Value: set of request types (<see cref="HashSet{Type}"/>)
    /// </summary>
    public static Dictionary<string, HashSet<Type>> CacheTags = [];

    /// <summary>
    /// Maps request types to response types.
    /// Key: request type (<see cref="Type"/>)
    /// Value: response type (<see cref="Type"/>)
    /// </summary>
    public static Dictionary<Type, Type> RequestResponseTypes = [];

    public static Type? GetResponseType<TRequest>()
    {   
        RequestResponseTypes.TryGetValue(typeof(TRequest), out var type);
        return type;
    }
    
    public static Result UpdateContainer<TRequest, TResponse>(IEnumerable<string>? tags = null, string? cacheKey = null)
    {
        try
        {
            var updateCacheTag = AddOrUpdateCacheTag<TRequest>(tags);
            var updateCacheType = AddOrUpdateCacheType<TRequest>(cacheKey);

            if (updateCacheTag.IsSuccess && updateCacheType.IsSuccess) return Result.Ok();
            
            RequestResponseTypes.Add(typeof(TRequest), typeof(TResponse));

            return Result.Fail(ErrorMessages.ContainerUpdatesFails);
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    private static Result AddOrUpdateCacheTag<TRequest>(IEnumerable<string>? tags = null)
    {
        try
        {
            if (tags == null)
                return Result.Ok();

            foreach (var tag in tags)
            {
                if (CacheTags.TryGetValue(tag, out HashSet<Type>? tagTypes))
                {
                    tagTypes ??= [];
                    tagTypes.Add(typeof(TRequest));
                }
                else
                {
                    tagTypes = [typeof(TRequest)];
                    CacheTags.TryAdd(tag, tagTypes);
                }
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }

    private static Result AddOrUpdateCacheType<TRequest>(string? cacheKey = null)
    {
        try
        {
            if (cacheKey == null)
                return Result.Ok();

            if (CacheTypes.TryGetValue(typeof(TRequest), out HashSet<string?>? cacheTypes))
            {
                cacheTypes ??= [];
                cacheTypes.Add(cacheKey);
            }
            else
            {
                cacheTypes = [cacheKey];
                CacheTypes.TryAdd(typeof(TRequest), cacheTypes);
            }

            return Result.Ok();
        }
        catch (Exception exception)
        {
            return Result.Fail(exception.Message);
        }
    }
}