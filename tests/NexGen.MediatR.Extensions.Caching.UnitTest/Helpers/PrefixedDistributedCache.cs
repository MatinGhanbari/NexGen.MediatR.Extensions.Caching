using Microsoft.Extensions.Caching.Distributed;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Helpers;

/// <summary>
/// Applies a key prefix to simulate Redis <c>InstanceName</c> isolation in multi-app deployments.
/// </summary>
internal sealed class PrefixedDistributedCache : IDistributedCache
{
    private readonly IDistributedCache _inner;
    private readonly string _prefix;

    public PrefixedDistributedCache(IDistributedCache inner, string prefix)
    {
        _inner = inner;
        _prefix = prefix;
    }

    private string Key(string key) => _prefix + key;

    public byte[]? Get(string key) => _inner.Get(Key(key));

    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
        _inner.GetAsync(Key(key), token);

    public void Refresh(string key) => _inner.Refresh(Key(key));

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        _inner.RefreshAsync(Key(key), token);

    public void Remove(string key) => _inner.Remove(Key(key));

    public Task RemoveAsync(string key, CancellationToken token = default) =>
        _inner.RemoveAsync(Key(key), token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        _inner.Set(Key(key), value, options);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        _inner.SetAsync(Key(key), value, options, token);
}
