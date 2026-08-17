using MediatR;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using NexGen.MediatR.Extensions.Caching.Containers;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Memory;

public sealed class MemoryOutputCacheTests
{
    private sealed record UserQuery(int Id) : IRequest<string>;
    private sealed record OrderQuery(int Id) : IRequest<string>;
    private sealed record ProfileQuery(int Id) : IRequest<string>;

    [Fact]
    public async Task SetAsync_ThenGetAsync_ReturnsCachedValue()
    {
        var cache = CreateCache<UserQuery>();
        var query = new UserQuery(1);

        Assert.True((await cache.SetAsync(query, "cached", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        var hit = await cache.GetAsync(query);
        Assert.True(hit.IsSuccess);
        Assert.Equal("cached", hit.Value);
    }

    [Fact]
    public async Task GetAsync_Miss_ReturnsFailedResult()
    {
        var cache = CreateCache<UserQuery>();
        var result = await cache.GetAsync(new UserQuery(99));
        Assert.True(result.IsFailed);
    }

    [Fact]
    public async Task EvictByTagsAsync_RemovesMatchingEntries()
    {
        var container = new RequestOutputCacheContainer();
        var cache = CreateCache<UserQuery>(container);
        var query = new UserQuery(5);

        Assert.True((await cache.SetAsync(query, "value", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await cache.EvictByTagsAsync(["User"])).IsSuccess);
        Assert.True((await cache.GetAsync(query)).IsFailed);
    }

    [Fact]
    public async Task EvictByTagsAsync_PartialTag_DoesNotEvictUnrelatedTag()
    {
        var container = new RequestOutputCacheContainer();
        var memory = new MemoryCache(new MemoryCacheOptions());
        var userCache = CreateCache<UserQuery>(container, memory);
        var orderCache = CreateCache<OrderQuery>(container, memory);

        var userQuery = new UserQuery(1);
        var orderQuery = new OrderQuery(1);

        Assert.True((await userCache.SetAsync(userQuery, "user", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await orderCache.SetAsync(orderQuery, "order", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await userCache.EvictByTagsAsync(["User"])).IsSuccess);

        Assert.True((await userCache.GetAsync(userQuery)).IsFailed);
        Assert.True((await orderCache.GetAsync(orderQuery)).IsSuccess);
        Assert.Equal("order", (await orderCache.GetAsync(orderQuery)).Value);
    }

    [Fact]
    public async Task EvictByTagsAsync_SharedTag_EvictsAllLinkedRequestTypes()
    {
        var container = new RequestOutputCacheContainer();
        var memory = new MemoryCache(new MemoryCacheOptions());
        var userCache = CreateCache<UserQuery>(container, memory);
        var profileCache = CreateCache<ProfileQuery>(container, memory);

        var userQuery = new UserQuery(1);
        var profileQuery = new ProfileQuery(1);

        Assert.True((await userCache.SetAsync(userQuery, "user", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await profileCache.SetAsync(profileQuery, "profile", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await userCache.EvictByTagsAsync(["User"])).IsSuccess);

        Assert.True((await userCache.GetAsync(userQuery)).IsFailed);
        Assert.True((await profileCache.GetAsync(profileQuery)).IsFailed);
    }

    [Fact]
    public async Task MultipleTags_OnSingleRequest_EvictAnyTagRemovesEntry()
    {
        var cache = CreateCache<UserQuery>();
        var query = new UserQuery(3);

        Assert.True((await cache.SetAsync(query, "multi", tags: ["User", "Profile"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await cache.EvictByTagsAsync(["Profile"])).IsSuccess);
        Assert.True((await cache.GetAsync(query)).IsFailed);
    }

    [Fact]
    public async Task SameRequestType_DifferentParameters_ProduceSeparateEntries()
    {
        var cache = CreateCache<UserQuery>();
        var queryA = new UserQuery(1);
        var queryB = new UserQuery(2);

        Assert.True((await cache.SetAsync(queryA, "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await cache.SetAsync(queryB, "b", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.Equal("a", (await cache.GetAsync(queryA)).Value);
        Assert.Equal("b", (await cache.GetAsync(queryB)).Value);

        Assert.NotEqual(
            RequestOutputCacheHelper.GetCacheKey(queryA),
            RequestOutputCacheHelper.GetCacheKey(queryB));
    }

    [Fact]
    public async Task FlushAll_RemovesAllCachedEntries()
    {
        var container = new RequestOutputCacheContainer();
        var memory = new MemoryCache(new MemoryCacheOptions());
        var userCache = CreateCache<UserQuery>(container, memory);
        var orderCache = CreateCache<OrderQuery>(container, memory);

        Assert.True((await userCache.SetAsync(new UserQuery(1), "u", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await orderCache.SetAsync(new OrderQuery(1), "o", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await userCache.FlushAll()).IsSuccess);

        Assert.True((await userCache.GetAsync(new UserQuery(1))).IsFailed);
        Assert.True((await orderCache.GetAsync(new OrderQuery(1))).IsFailed);
    }

    [Fact]
    public async Task EvictByTagsAsync_UnknownTag_IsNoOp()
    {
        var cache = CreateCache<UserQuery>();
        var query = new UserQuery(1);
        Assert.True((await cache.SetAsync(query, "x", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await cache.EvictByTagsAsync(["NonExistent"])).IsSuccess);
        Assert.Equal("x", (await cache.GetAsync(query)).Value);
    }

    private static RequestOutputCache<TRequest, string> CreateCache<TRequest>(
        RequestOutputCacheContainer? container = null,
        IMemoryCache? memoryCache = null)
        where TRequest : IRequest<string>
    {
        container ??= new RequestOutputCacheContainer();
        memoryCache ??= new MemoryCache(new MemoryCacheOptions());
        return new RequestOutputCache<TRequest, string>(
            NullLogger<RequestOutputCache<TRequest, string>>.Instance,
            memoryCache,
            container);
    }
}
