using NexGen.MediatR.Extensions.Caching.Constants;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Constants;

internal static class CacheKeys
{
    private const string Root = RequestCacheConstants.CacheKeyRootPrefix;

    internal const string CacheTypesKey = Root + ":Container:CacheTypes";
    internal const string CacheTagsKey = Root + ":Container:CacheTags";
    internal const string RequestResponseTypesKey = Root + ":Container:RequestResponseTypes";
    internal const string EvictionChannel = Root + ":Evict";
}
