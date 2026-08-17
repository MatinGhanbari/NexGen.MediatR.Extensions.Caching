using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Redis.Configurations;

/// <summary>
/// Configuration options for the Redis MediatR request output cache provider.
/// </summary>
public sealed class RedisRequestOutputCacheOptions
{
    /// <summary>
    /// Redis connection string. Required unless <see cref="ConfigurationOptions"/> is set.
    /// </summary>
    public string? ConnectionString { get; set; }
 
    /// <summary>
    /// Optional key prefix applied by <c>IDistributedCache</c> (<c>RedisCacheOptions.InstanceName</c>).
    /// When set, the eviction Pub/Sub channel is also prefixed so co-tenant apps do not cross-evict.
    /// A trailing <c>:</c> is ensured automatically (e.g. <c>my-app</c> becomes <c>my-app:</c>).
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Optional Redis database index (<c>ConfigurationOptions.DefaultDatabase</c>).
    /// </summary>
    public int? Database { get; set; }

    /// <summary>
    /// Optional default cache lifetime in seconds for requests that omit an explicit
    /// <c>expirationInSeconds</c> on the cache attribute.
    /// </summary>
    public int? DefaultExpirationInSeconds { get; set; }

    /// <summary>
    /// Optional advanced StackExchange.Redis configuration.
    /// When set, takes precedence over <see cref="ConnectionString"/> for connection settings.
    /// </summary>
    public ConfigurationOptions? ConfigurationOptions { get; set; }

    /// <summary>
    /// When <see langword="true"/> (the default), this host publishes and subscribes to Redis Pub/Sub
    /// so other services using the same cache prefix invalidate matching tags.
    /// Set to <see langword="false"/> to keep eviction local to this process.
    /// </summary>
    public bool EnableDistributedEviction { get; set; } = true;

    /// <summary>
    /// Optional Pub/Sub channel for eviction notifications.
    /// Defaults to <c>NexGen.MediatR.Extensions.Caching:Evict</c>, prefixed by <see cref="InstanceName"/> when set.
    /// </summary>
    public string? EvictionChannel { get; set; }
}
