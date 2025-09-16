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
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        // Serialize the request to JSON
        var serialized = JsonConvert.SerializeObject(request);

        // Compute SHA-256 hash of the serialized request
        var source = Encoding.UTF8.GetBytes(serialized);
        var hashBytes = SHA256.HashData(source);

        // Convert hash bytes to a lowercase hexadecimal string
        var hashString = BitConverter
            .ToString(hashBytes)
            .Replace("-", "")
            .ToLowerInvariant();

        // Combine request type name with hash to create the cache key
        return $"{typeof(TRequest).Name}:{hashString}";
    }
}