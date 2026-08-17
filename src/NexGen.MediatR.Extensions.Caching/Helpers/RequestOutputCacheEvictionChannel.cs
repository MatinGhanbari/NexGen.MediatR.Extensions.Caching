using NexGen.MediatR.Extensions.Caching.Constants;

namespace NexGen.MediatR.Extensions.Caching.Helpers;

/// <summary>
/// Resolves the Redis/Garnet Pub/Sub channel used for distributed tag eviction.
/// </summary>
internal static class RequestOutputCacheEvictionChannel
{
    internal static string Resolve(string? instanceName, string? evictionChannel)
    {
        var channel = string.IsNullOrWhiteSpace(evictionChannel)
            ? RequestCacheConstants.CacheKeyRootPrefix + ":Evict"
            : evictionChannel.Trim();

        var prefix = RequestOutputCacheInstanceNameNormalizer.Normalize(instanceName);
        return prefix is null ? channel : prefix + channel;
    }
}
