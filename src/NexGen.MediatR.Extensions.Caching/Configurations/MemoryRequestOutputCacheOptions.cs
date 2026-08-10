namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Configuration options for the in-memory MediatR request output cache provider.
/// </summary>
public sealed class MemoryRequestOutputCacheOptions
{
    /// <summary>
    /// Optional default cache lifetime in seconds for requests that omit an explicit
    /// <c>expirationInSeconds</c> on <see cref="Attributes.RequestOutputCacheAttribute"/>.
    /// </summary>
    public int? DefaultExpirationInSeconds { get; set; }
}
