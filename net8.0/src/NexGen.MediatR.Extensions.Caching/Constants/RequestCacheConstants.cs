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
    public const int ExpirationInSeconds = 300;
}