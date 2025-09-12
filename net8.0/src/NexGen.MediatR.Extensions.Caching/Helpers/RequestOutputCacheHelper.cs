using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace NexGen.MediatR.Extensions.Caching.Helpers;

public static class RequestOutputCacheHelper
{
    public static string GetCacheKey<TRequest>(TRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        var serialized = JsonConvert.SerializeObject(request);

        using var sha = SHA256.Create();
        var hashBytes = sha.ComputeHash(Encoding.UTF8.GetBytes(serialized));
        var hashString = BitConverter
            .ToString(hashBytes)
            .Replace("-", "")
            .ToLowerInvariant();

        return $"{typeof(TRequest).Name}:{hashString}";
    }
}