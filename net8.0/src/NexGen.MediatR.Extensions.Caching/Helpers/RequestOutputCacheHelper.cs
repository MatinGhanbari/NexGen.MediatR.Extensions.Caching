using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace NexGen.MediatR.Extensions.Caching.Helpers;

/// <summary>
/// Provides helper methods for generating cache keys for MediatR requests.
/// </summary>
public static class RequestOutputCacheHelper
{
    /// <summary>
    /// Generates a unique cache key for the specified request by serializing it
    /// and computing the SHA-256 hash.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <param name="request">The request object to generate a cache key for.</param>
    /// <returns>A string representing a unique cache key for the request.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the request is null.</exception>
    public static string GetCacheKey<TRequest>(TRequest request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        var serialized = JsonConvert.SerializeObject(request);
        var source = Encoding.UTF8.GetBytes(serialized);
        var hashBytes = SHA256.HashData(source);

        var hashString = BitConverter
            .ToString(hashBytes)
            .Replace("-", "")
            .ToLowerInvariant();

        return $"{typeof(TRequest).Name}:{hashString}";
    }
}