using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using NexGen.MediatR.Extensions.Caching.Redis.Containers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Helpers;

/// <summary>
/// Emulates the server-side compare-and-swap the Redis provider performs through scripting:
/// a write only lands when the stored document still matches the one the caller merged from.
/// </summary>
internal sealed class RedisCompareAndSwapIndexStore : IContainerIndexStore
{
    private readonly IDistributedCache _readCache;
    private readonly IDistributedCache _authoritative;
    private readonly object _gate;

    public RedisCompareAndSwapIndexStore(IDistributedCache readCache, IDistributedCache authoritative, object gate)
    {
        _readCache = readCache;
        _authoritative = authoritative;
        _gate = gate;
    }

    public Task<string?> ReadAsync(string key, CancellationToken cancellationToken) =>
        _readCache.GetStringAsync(key, cancellationToken);

    public Task<bool> TryUpdateAsync(string key, string? expected, string? updated, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var stored = _authoritative.Get(key);
            var current = stored is null ? null : Encoding.UTF8.GetString(stored);

            if (!string.Equals(current, expected, StringComparison.Ordinal))
                return Task.FromResult(false);

            if (updated is null)
                _authoritative.Remove(key);
            else
                _authoritative.SetString(key, updated);

            return Task.FromResult(true);
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _authoritative.Remove(key);
        }

        return Task.CompletedTask;
    }
}
