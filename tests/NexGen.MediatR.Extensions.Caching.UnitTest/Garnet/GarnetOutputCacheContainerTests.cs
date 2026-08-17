using NexGen.MediatR.Extensions.Caching.UnitTest.Helpers;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Garnet;
using NexGen.MediatR.Extensions.Caching.Garnet.Containers;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Garnet;

/// <summary>
/// Garnet mirrors Redis container serialization; keep critical multi-app paths covered here.
/// </summary>
public sealed class GarnetOutputCacheContainerTests
{
    private static readonly string RequestResponseTypesKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:RequestResponseTypes";

    private static readonly string CacheTypesKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:CacheTypes";

    private static readonly string CacheTagsKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:CacheTags";

    private sealed record LocalQuery(int Id) : IRequest<string>;
    private sealed record AppAQuery(int Id) : IRequest<string>;
    private sealed record AppBQuery(string Name) : IRequest<string>;
    private sealed record AppCQuery(int Id) : IRequest<string>;

    [Fact]
    public async Task Concurrent_UpdateContainer_KeepsPeerMetadataInAllIndexes()
    {
        var store = new InMemoryDistributedCache();
        var gate = new object();
        var coordinated = new CoordinatedReadDistributedCache(
            store,
            readersBeforeRelease: 2,
            CacheTagsKey,
            CacheTypesKey,
            RequestResponseTypesKey);

        var containerA = new GarnetOutputCacheContainer(new GarnetCompareAndSwapIndexStore(coordinated, store, gate));
        var containerB = new GarnetOutputCacheContainer(new GarnetCompareAndSwapIndexStore(coordinated, store, gate));

        var results = await Task.WhenAll(
            containerA.UpdateContainerAsync<AppAQuery>(tags: ["User"], cacheKey: "a-key", responseType: typeof(string)),
            containerB.UpdateContainerAsync<AppBQuery>(tags: ["Order"], cacheKey: "b-key", responseType: typeof(string)));

        Assert.All(results, result => Assert.True(result.IsSuccess));

        var container = new GarnetOutputCacheContainer(store);

        var cacheTags = await container.GetCacheTagsAsync();
        Assert.Contains(typeof(AppAQuery).FullName!, cacheTags["User"]);
        Assert.Contains(typeof(AppBQuery).FullName!, cacheTags["Order"]);

        var cacheTypes = await container.GetCacheTypesAsync();
        Assert.Contains("a-key", cacheTypes[typeof(AppAQuery).FullName!]);
        Assert.Contains("b-key", cacheTypes[typeof(AppBQuery).FullName!]);

        Assert.Equal(typeof(string), await container.GetResponseTypeAsync<AppAQuery>());
        Assert.Equal(typeof(string), await container.GetResponseTypeAsync<AppBQuery>());
    }

    [Fact]
    public async Task ParallelWriters_KeepAllMetadata_AndEvictionClearsIndexes()
    {
        var store = new InMemoryDistributedCache();
        var gate = new object();
        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        async Task<bool> WriteAsync<TRequest>(string cacheKey)
        {
            var container = new GarnetOutputCacheContainer(new GarnetCompareAndSwapIndexStore(store, store, gate));
            await start.Task;
            var result = await container.UpdateContainerAsync<TRequest>(
                tags: ["User"],
                cacheKey: cacheKey,
                responseType: typeof(string));
            return result.IsSuccess;
        }

        var writers = new[]
        {
            WriteAsync<AppAQuery>("key-a"),
            WriteAsync<AppBQuery>("key-b"),
            WriteAsync<AppCQuery>("key-c"),
            WriteAsync<LocalQuery>("key-d")
        };

        start.SetResult();
        Assert.All(await Task.WhenAll(writers), Assert.True);

        var container = new GarnetOutputCacheContainer(store);
        var requestTypeNames = new[]
        {
            typeof(AppAQuery).FullName!,
            typeof(AppBQuery).FullName!,
            typeof(AppCQuery).FullName!,
            typeof(LocalQuery).FullName!
        };

        var cacheTags = await container.GetCacheTagsAsync();
        var cacheTypes = await container.GetCacheTypesAsync();
        foreach (var requestTypeName in requestTypeNames)
        {
            Assert.Contains(requestTypeName, cacheTags["User"]);
            Assert.Contains(requestTypeName, cacheTypes.Keys);
        }

        var evicted = await CreateCache<AppAQuery>(store).EvictByTagsAsync(["User"]);
        Assert.True(evicted.IsSuccess);

        Assert.Empty(await container.GetCacheTagsAsync());
        Assert.Empty(await container.GetCacheTypesAsync());
        Assert.Null(await store.GetStringAsync(RequestResponseTypesKey));
    }

    [Fact]
    public async Task SetAsync_WithForeignTypeEntries_StillWritesResponse()
    {
        var store = new InMemoryDistributedCache();
        var foreignMap = new Dictionary<string, string>
        {
            ["OtherService.Queries.ForeignQuery, OtherService"] = "OtherService.Dtos.ForeignDto, OtherService"
        };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(foreignMap));

        var cache = new GarnetRequestOutputCache<LocalQuery, string>(
            NullLogger<GarnetRequestOutputCache<LocalQuery, string>>.Instance,
            store,
            new GarnetOutputCacheContainer(store));

        var query = new LocalQuery(7);
        var set = await cache.SetAsync(query, "payload", tags: ["User"], expirationInSeconds: 60);
        Assert.True(set.IsSuccess);

        var cacheKey = RequestOutputCacheHelper.GetCacheKey(query);
        Assert.Equal(JsonConvert.SerializeObject("payload"), await store.GetStringAsync(cacheKey));

        var hit = await cache.GetAsync(query);
        Assert.True(hit.IsSuccess);
        Assert.Equal("payload", hit.Value);
    }

    [Fact]
    public async Task UpdateContainer_PersistsSecondCacheKey_ForExistingRequestType()
    {
        var store = new InMemoryDistributedCache();
        var container = new GarnetOutputCacheContainer(store);

        Assert.True((await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"], cacheKey: "key-first", responseType: typeof(string))).IsSuccess);
        Assert.True((await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"], cacheKey: "key-second", responseType: typeof(string))).IsSuccess);

        var cacheTypes = JsonConvert.DeserializeObject<Dictionary<string, HashSet<string?>>>(
            (await store.GetStringAsync(CacheTypesKey))!)!;
        var keys = cacheTypes[typeof(LocalQuery).FullName!];
        Assert.Contains("key-first", keys);
        Assert.Contains("key-second", keys);
    }

    [Fact]
    public async Task EvictByTags_RemovesEvictedRequestTypeFromAllIndexes()
    {
        var store = new InMemoryDistributedCache();
        var cache = CreateCache<LocalQuery>(store);
        var query = new LocalQuery(11);

        Assert.True((await cache.SetAsync(query, "payload", tags: ["User", "Admin"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await cache.EvictByTagsAsync(["User"])).IsSuccess);

        var container = new GarnetOutputCacheContainer(store);
        Assert.Empty(await container.GetCacheTagsAsync());
        Assert.Empty(await container.GetCacheTypesAsync());
        Assert.Null(await container.GetResponseTypeAsync<LocalQuery>());
        Assert.Null(await store.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(query)));
        Assert.Null(await store.GetStringAsync(CacheTagsKey));
        Assert.Null(await store.GetStringAsync(CacheTypesKey));
        Assert.Null(await store.GetStringAsync(RequestResponseTypesKey));
    }

    [Fact]
    public async Task FlushAll_RemovesValuesAndIndexes()
    {
        var store = new InMemoryDistributedCache();
        var cache = CreateCache<LocalQuery>(store);
        var query = new LocalQuery(12);
        Assert.True((await cache.SetAsync(query, "payload", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await cache.FlushAll()).IsSuccess);

        Assert.Null(await store.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(query)));
        Assert.Null(await store.GetStringAsync(CacheTagsKey));
        Assert.Null(await store.GetStringAsync(CacheTypesKey));
        Assert.Null(await store.GetStringAsync(RequestResponseTypesKey));
    }

    [Fact]
    public async Task TwoApps_SameStore_BothCanSetAndGet()
    {
        var shared = new InMemoryDistributedCache();
        var appA = CreateCache<AppAQuery>(shared);
        var appB = CreateCache<AppBQuery>(shared);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");
        Assert.True((await appA.SetAsync(aQuery, "a-payload", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b-payload", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        Assert.Equal("a-payload", (await appA.GetAsync(aQuery)).Value);
        Assert.Equal("b-payload", (await appB.GetAsync(bQuery)).Value);
    }

    [Fact]
    public async Task GetResponseType_ResolvesLegacyAssemblyQualifiedNameKey()
    {
        var store = new InMemoryDistributedCache();
        var legacy = new Dictionary<string, string>
        {
            [typeof(LocalQuery).AssemblyQualifiedName!] = typeof(string).AssemblyQualifiedName!
        };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(legacy));

        var resolved = await new GarnetOutputCacheContainer(store).GetResponseTypeAsync<LocalQuery>();
        Assert.Equal(typeof(string), resolved);
    }

    [Fact]
    public async Task TwoApps_DifferentInstanceName_ContainersAreIsolated()
    {
        var shared = new InMemoryDistributedCache();
        var appAStore = new PrefixedDistributedCache(shared, "app-a:");
        var appBStore = new PrefixedDistributedCache(shared, "app-b:");

        var appA = CreateCache<AppAQuery>(appAStore);
        var appB = CreateCache<AppBQuery>(appBStore);

        Assert.True((await appA.SetAsync(new AppAQuery(1), "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(new AppBQuery("x"), "b", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.NotNull(await shared.GetStringAsync("app-a:" + RequestResponseTypesKey));
        Assert.NotNull(await shared.GetStringAsync("app-b:" + RequestResponseTypesKey));
        Assert.Null(await shared.GetStringAsync(RequestResponseTypesKey));
    }

    [Fact]
    public async Task SharedTag_WithoutInstanceName_EvictAffectsBothApps()
    {
        var shared = new InMemoryDistributedCache();
        var appA = CreateCache<AppAQuery>(shared);
        var appB = CreateCache<AppBQuery>(shared);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");
        Assert.True((await appA.SetAsync(aQuery, "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await appA.EvictByTagsAsync(["User"])).IsSuccess);

        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(aQuery)));
        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(bQuery)));
    }

    [Fact]
    public async Task SharedTag_WithInstanceName_EvictIsIsolated()
    {
        var shared = new InMemoryDistributedCache();
        var appAStore = new PrefixedDistributedCache(shared, "app-a:");
        var appBStore = new PrefixedDistributedCache(shared, "app-b:");
        var appA = CreateCache<AppAQuery>(appAStore);
        var appB = CreateCache<AppBQuery>(appBStore);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");
        Assert.True((await appA.SetAsync(aQuery, "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await appA.EvictByTagsAsync(["User"])).IsSuccess);

        Assert.Null(await appAStore.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(aQuery)));
        Assert.Equal(
            JsonConvert.SerializeObject("b"),
            await appBStore.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(bQuery)));
    }

    [Fact]
    public async Task FlushAll_WithoutInstanceName_RemovesOtherAppEntries()
    {
        var shared = new InMemoryDistributedCache();
        var appA = CreateCache<AppAQuery>(shared);
        var appB = CreateCache<AppBQuery>(shared);

        Assert.True((await appA.SetAsync(new AppAQuery(1), "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(new AppBQuery("x"), "b", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await appA.FlushAll()).IsSuccess);

        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(new AppAQuery(1))));
        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(new AppBQuery("x"))));
        Assert.Null(await shared.GetStringAsync(CacheTagsKey));
        Assert.Null(await shared.GetStringAsync(CacheTypesKey));
        Assert.Null(await shared.GetStringAsync(RequestResponseTypesKey));
    }

    private static GarnetRequestOutputCache<TRequest, string> CreateCache<TRequest>(IDistributedCache store)
        where TRequest : IRequest<string> =>
        new(
            NullLogger<GarnetRequestOutputCache<TRequest, string>>.Instance,
            store,
            new GarnetOutputCacheContainer(store));
}
