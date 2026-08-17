using Microsoft.Extensions.Caching.Distributed;

namespace NexGen.MediatR.Extensions.Caching.Garnet.Containers;

/// <summary>
/// Index store without compare-and-swap: the last writer of a document wins.
/// </summary>
internal sealed class DistributedCacheIndexStore : IContainerIndexStore
{
    private readonly IDistributedCache _cache;

    public DistributedCacheIndexStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public Task<string?> ReadAsync(string key, CancellationToken cancellationToken) =>
        _cache.GetStringAsync(key, cancellationToken);

    public async Task<bool> TryUpdateAsync(string key, string? expected, string? updated, CancellationToken cancellationToken)
    {
        if (updated is null)
            await _cache.RemoveAsync(key, cancellationToken).ConfigureAwait(false);
        else
            await _cache.SetStringAsync(key, updated, cancellationToken).ConfigureAwait(false);

        return true;
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
        _cache.RemoveAsync(key, cancellationToken);
}
