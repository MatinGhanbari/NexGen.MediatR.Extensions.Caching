namespace NexGen.MediatR.Extensions.Caching.Helpers;

/// <summary>
/// Normalizes cache tags so cache-side and eviction-side values match.
/// </summary>
internal static class RequestOutputCacheTagNormalizer
{
    /// <summary>
    /// Trims tags, drops empty values, and de-duplicates using ordinal comparison.
    /// </summary>
    internal static string[] Normalize(IEnumerable<string>? tags)
    {
        if (tags is null)
            return [];

        HashSet<string>? seen = null;
        List<string>? result = null;

        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;

            var normalized = tag.Trim();
            seen ??= new HashSet<string>(StringComparer.Ordinal);
            if (!seen.Add(normalized))
                continue;

            result ??= [];
            result.Add(normalized);
        }

        return result is null ? [] : result.ToArray();
    }
}
