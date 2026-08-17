using Microsoft.Extensions.Caching.Distributed;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Helpers;

/// <summary>
/// Holds reads of the coordinated keys until the expected number of readers has taken a snapshot,
/// so concurrent container writers all merge from the same pre-write view.
/// </summary>
internal sealed class CoordinatedReadDistributedCache : IDistributedCache
{
    private static readonly TimeSpan ReaderTimeout = TimeSpan.FromSeconds(5);

    private readonly IDistributedCache _inner;
    private readonly Dictionary<string, ReaderBarrier> _barriers;

    public CoordinatedReadDistributedCache(IDistributedCache inner, int readersBeforeRelease, params string[] coordinatedKeys)
    {
        _inner = inner;
        _barriers = coordinatedKeys.ToDictionary(
            key => key,
            _ => new ReaderBarrier(readersBeforeRelease),
            StringComparer.Ordinal);
    }

    public byte[]? Get(string key) => _inner.Get(key);

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        if (!_barriers.TryGetValue(key, out var barrier))
            return await _inner.GetAsync(key, token).ConfigureAwait(false);

        var snapshot = await _inner.GetAsync(key, token).ConfigureAwait(false);
        await barrier.SignalAndWaitAsync().ConfigureAwait(false);
        return snapshot;
    }

    public void Refresh(string key) => _inner.Refresh(key);

    public Task RefreshAsync(string key, CancellationToken token = default) =>
        _inner.RefreshAsync(key, token);

    public void Remove(string key) => _inner.Remove(key);

    public Task RemoveAsync(string key, CancellationToken token = default) =>
        _inner.RemoveAsync(key, token);

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        _inner.Set(key, value, options);

    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) =>
        _inner.SetAsync(key, value, options, token);

    private sealed class ReaderBarrier
    {
        private readonly int _readersBeforeRelease;
        private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _readers;

        public ReaderBarrier(int readersBeforeRelease)
        {
            _readersBeforeRelease = readersBeforeRelease;
        }

        public Task SignalAndWaitAsync()
        {
            if (Interlocked.Increment(ref _readers) >= _readersBeforeRelease)
                _release.TrySetResult();

            // Time out instead of hanging the suite when a peer never reads this key.
            return Task.WhenAny(_release.Task, Task.Delay(ReaderTimeout));
        }
    }
}
