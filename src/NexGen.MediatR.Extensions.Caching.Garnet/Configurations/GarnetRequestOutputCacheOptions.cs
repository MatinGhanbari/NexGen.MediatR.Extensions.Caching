using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Configurations;

/// <summary>
/// Configuration options for the Garnet MediatR request output cache provider.
/// </summary>
public sealed class GarnetRequestOutputCacheOptions
{
    /// <summary>
    /// Garnet connection string. Required unless <see cref="ConfigurationOptions"/> is set.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Optional key prefix applied by <c>IDistributedCache</c> (<c>RedisCacheOptions.InstanceName</c>).
    /// </summary>
    public string? InstanceName { get; set; }

    /// <summary>
    /// Optional Redis-compatible database index (<c>ConfigurationOptions.DefaultDatabase</c>).
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
}