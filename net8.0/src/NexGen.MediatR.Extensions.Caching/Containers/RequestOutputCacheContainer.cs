namespace NexGen.MediatR.Extensions.Caching.Containers;

public static class RequestOutputCacheContainer
{
    public static Dictionary<Type, HashSet<string>> CacheTypes = [];
    public static Dictionary<string, HashSet<Type>> CacheTags = [];
}