using StackExchange.Redis;

namespace NexGen.MediatR.Extensions.Caching.Redis.Containers;

/// <summary>
/// Index store that swaps documents through a server-side script, so replicas sharing one
/// Redis instance merge the container indexes without losing each other's entries.
/// </summary>
internal sealed class RedisIndexStore : IContainerIndexStore
{
    /// <summary>
    /// Entries are hashes with <c>absexp</c>, <c>sldexp</c> and <c>data</c> fields, matching the layout
    /// <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/> reads and writes.
    /// Index documents never expire, so both expiration fields stay at <c>-1</c>.
    /// </summary>
    private const string CompareAndSwapScript =
        """
        local current = redis.call('HGET', KEYS[1], 'data')
        if ARGV[1] == '1' then
            if current == false or current ~= ARGV[2] then return 0 end
        elseif current ~= false then
            return 0
        end
        if ARGV[3] == '1' then
            redis.call('DEL', KEYS[1])
        else
            redis.call('HSET', KEYS[1], 'absexp', '-1', 'sldexp', '-1', 'data', ARGV[4])
        end
        return 1
        """;

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly string _keyPrefix;
    private readonly IContainerIndexStore _fallback;
    private volatile bool _scriptingUnavailable;

    public RedisIndexStore(
        IConnectionMultiplexer connectionMultiplexer,
        string? keyPrefix,
        IContainerIndexStore fallback)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _keyPrefix = keyPrefix ?? string.Empty;
        _fallback = fallback;
    }

    public Task<string?> ReadAsync(string key, CancellationToken cancellationToken) =>
        _fallback.ReadAsync(key, cancellationToken);

    public async Task<bool> TryUpdateAsync(string key, string? expected, string? updated, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (_scriptingUnavailable)
            return await _fallback.TryUpdateAsync(key, expected, updated, cancellationToken).ConfigureAwait(false);

        try
        {
            var result = await _connectionMultiplexer.GetDatabase().ScriptEvaluateAsync(
                CompareAndSwapScript,
                [(RedisKey)(_keyPrefix + key)],
                [
                    expected is null ? "0" : "1",
                    expected ?? string.Empty,
                    updated is null ? "1" : "0",
                    updated ?? string.Empty
                ]).ConfigureAwait(false);

            return (long)result == 1;
        }
        catch (RedisServerException)
        {
            // Server without scripting support: keep caching working, without compare-and-swap.
            _scriptingUnavailable = true;
            return await _fallback.TryUpdateAsync(key, expected, updated, cancellationToken).ConfigureAwait(false);
        }
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken) =>
        _fallback.RemoveAsync(key, cancellationToken);
}
