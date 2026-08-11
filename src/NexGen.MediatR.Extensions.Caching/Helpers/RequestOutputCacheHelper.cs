using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using System.Security.Cryptography;
using System.Text;

namespace NexGen.MediatR.Extensions.Caching.Helpers;

/// <summary>
/// Provides helper methods for generating cache keys for MediatR requests.
/// </summary>
public static class RequestOutputCacheHelper
{
    /// <summary>
    /// Generates a unique, human-readable cache key for the specified request by serializing it
    /// and computing the SHA-256 hash. The key includes the library root prefix, the request
    /// namespace and nested declaring types from <see cref="Type.FullName"/> (with <c>.</c> and <c>+</c>
    /// replaced by <c>:</c> for Redis tree browsing), and the type path
    /// so requests that share a short name in different namespaces do not collide.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <param name="request">The request object to generate a cache key for.</param>
    /// <returns>
    /// A string in the form
    /// <c>NexGen.MediatR.Extensions:{Type.FullName with : separators}:{sha256Hex}</c>.
    /// </returns>
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

        var requestType = typeof(TRequest);
        var typePath = BuildTypePath(requestType);

        return $"{RequestCacheConstants.CacheKeyRootPrefix}:{typePath}:{hashString}";
    }

    /// <summary>
    /// Builds a Redis-tree-friendly type path from the request CLR type.
    /// Uses <see cref="Type.FullName"/> when available (namespace + nested declaring types),
    /// replacing <c>.</c> and <c>+</c> with <c>:</c>. Falls back to namespace + name for open/constructed generics.
    /// </summary>
    private static string BuildTypePath(Type requestType)
    {
        var fullName = requestType.FullName;
        if (!string.IsNullOrEmpty(fullName) && fullName.IndexOf('[') < 0)
            return fullName.Replace('.', ':').Replace('+', ':');

        var typeName = requestType.Name.Replace('+', ':');
        var ns = requestType.Namespace;

        if (string.IsNullOrEmpty(ns))
            return typeName;

        return $"{ns.Replace('.', ':')}:{typeName}";
    }
}
