namespace NexGen.MediatR.Extensions.Caching.Enums;

/// <summary>
/// Represents the available types of caches supported by the MediatR output caching library.
/// </summary>
public enum RequestOutputCacheType
{
    /// <summary>
    /// Uses in-memory caching provided by <see cref="Microsoft.Extensions.Caching.Memory.IMemoryCache"/>.
    /// </summary>
    MemoryCache = 1,

    /// <summary>
    /// Uses a distributed Redis cache.
    /// </summary>
    RedisCache = 2,

    /// <summary>
    /// Uses a distributed Garnet cache.
    /// </summary>
    GarnetCache = 3,
}