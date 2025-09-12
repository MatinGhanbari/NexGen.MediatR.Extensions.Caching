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
    public static Dictionary<Type, HashSet<string>> CacheTypes = [];

    /// <summary>
    /// Maps cache tags to a set of request types that use them.
    /// Key: cache tag (<see cref="string"/>)
    /// Value: set of request types (<see cref="HashSet{Type}"/>)
    /// </summary>
    public static Dictionary<string, HashSet<Type>> CacheTags = [];
}