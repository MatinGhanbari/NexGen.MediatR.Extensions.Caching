namespace NexGen.MediatR.Extensions.Caching.Constants;

/// <summary>
/// Contains constants used by the MediatR output caching library.
/// </summary>
public static class RequestCacheConstants
{
    /// <summary>
    /// Default expiration time for cached responses, in seconds.
    /// Use this value if no custom expiration is specified.
    /// </summary>
    public const int DefaultExpirationInSeconds = 300;

    /// <summary>
    /// Root prefix for all library cache keys (response entries, container indexes, eviction channel).
    /// Uses <c>:</c> as the Redis hierarchy separator so keys appear under a single tree.
    /// </summary>
    public const string CacheKeyRootPrefix = "NexGen.MediatR.Extensions";
}