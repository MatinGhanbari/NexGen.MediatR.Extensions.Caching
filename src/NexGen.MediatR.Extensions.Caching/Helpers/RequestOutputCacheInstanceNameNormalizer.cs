namespace NexGen.MediatR.Extensions.Caching.Helpers;

/// <summary>
/// Normalizes Redis/Garnet <c>InstanceName</c> prefixes so key and channel prefixes stay consistent.
/// </summary>
internal static class RequestOutputCacheInstanceNameNormalizer
{
    /// <summary>
    /// Trims whitespace and ensures the value ends with exactly one <c>:</c>.
    /// Returns <see langword="null"/> when the input is null, empty, or only colons/whitespace.
    /// </summary>
    internal static string? Normalize(string? instanceName)
    {
        if (string.IsNullOrWhiteSpace(instanceName))
            return null;

        var trimmed = instanceName.Trim().TrimEnd(':');
        return trimmed.Length == 0 ? null : trimmed + ":";
    }
}
