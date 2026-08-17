using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Helpers;
using NexGen.MediatR.Extensions.Caching.Redis;
using NexGen.MediatR.Extensions.Caching.Redis.Containers;
using NexGen.MediatR.Extensions.Caching.UnitTest.Helpers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Redis;

public sealed class RedisOutputCacheContainerTests
{
    private static readonly string RequestResponseTypesKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:RequestResponseTypes";

    private static readonly string CacheTypesKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:CacheTypes";

    private static readonly string CacheTagsKey =
        RequestCacheConstants.CacheKeyRootPrefix + ":Container:CacheTags";

    private sealed record AppAQuery(int Id) : IRequest<string>;
    private sealed record AppBQuery(string Name) : IRequest<string>;
    private sealed record AppCQuery(int Id) : IRequest<string>;
    private sealed record LocalQuery(int Id) : IRequest<string>;

    [Fact]
    public async Task UpdateContainer_WithForeignTypeEntries_DoesNotFail()
    {
        var store = new InMemoryDistributedCache();
        var foreignMap = new Dictionary<string, string>
        {
            ["OtherService.Queries.ForeignQuery, OtherService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"] =
                "OtherService.Dtos.ForeignDto, OtherService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
        };

        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(foreignMap));

        var container = new RedisOutputCacheContainer(store);
        var result = await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"],
            cacheKey: "key-1",
            responseType: typeof(string));

        Assert.True(result.IsSuccess);

        var stored = await store.GetStringAsync(RequestResponseTypesKey);
        Assert.NotNull(stored);
        var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(stored!)!;
        Assert.Contains(foreignMap.Keys.First(), map.Keys);
        Assert.Contains(typeof(LocalQuery).FullName!, map.Keys);
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

        var cache = CreateCache<LocalQuery>(store);

        var query = new LocalQuery(7);
        var set = await cache.SetAsync(query, "payload", tags: ["User"], expirationInSeconds: 60);
        Assert.True(set.IsSuccess);

        var cacheKey = RequestOutputCacheHelper.GetCacheKey(query);
        var response = await store.GetStringAsync(cacheKey);
        Assert.Equal(JsonConvert.SerializeObject("payload"), response);

        var hit = await cache.GetAsync(query);
        Assert.True(hit.IsSuccess);
        Assert.Equal("payload", hit.Value);
    }

    [Fact]
    public async Task UpdateContainer_PersistsSecondCacheKey_ForExistingRequestType()
    {
        var store = new InMemoryDistributedCache();
        var container = new RedisOutputCacheContainer(store);

        var first = await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"],
            cacheKey: "key-first",
            responseType: typeof(string));
        Assert.True(first.IsSuccess);

        var second = await container.UpdateContainerAsync<LocalQuery>(
            tags: ["User"],
            cacheKey: "key-second",
            responseType: typeof(string));
        Assert.True(second.IsSuccess);

        var typesJson = await store.GetStringAsync(CacheTypesKey);
        Assert.NotNull(typesJson);
        var cacheTypes = JsonConvert.DeserializeObject<Dictionary<string, HashSet<string?>>>(typesJson!)!;
        var requestTypeName = typeof(LocalQuery).FullName!;
        Assert.True(cacheTypes.TryGetValue(requestTypeName, out var keys));
        Assert.Contains("key-first", keys!);
        Assert.Contains("key-second", keys!);
    }

    [Fact]
    public async Task EvictByTags_RemovesEvictedRequestTypeFromAllIndexes()
    {
        var store = new InMemoryDistributedCache();
        var cache = CreateCache<LocalQuery>(store);
        var query = new LocalQuery(11);

        Assert.True((await cache.SetAsync(query, "payload", tags: ["User", "Admin"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await cache.EvictByTagsAsync(["User"])).IsSuccess);

        var container = new RedisOutputCacheContainer(store);
        Assert.Empty(await container.GetCacheTagsAsync());
        Assert.Empty(await container.GetCacheTypesAsync());
        Assert.Null(await container.GetResponseTypeAsync<LocalQuery>());
        Assert.Null(await store.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(query)));
        Assert.Null(await store.GetStringAsync(CacheTagsKey));
        Assert.Null(await store.GetStringAsync(CacheTypesKey));
        Assert.Null(await store.GetStringAsync(RequestResponseTypesKey));
    }

    [Fact]
    public async Task EvictByTags_LeavesUnrelatedRequestTypeIndexes()
    {
        var store = new InMemoryDistributedCache();
        var userCache = CreateCache<AppAQuery>(store);
        var orderCache = CreateCache<AppBQuery>(store);
        var userQuery = new AppAQuery(1);
        var orderQuery = new AppBQuery("x");

        Assert.True((await userCache.SetAsync(userQuery, "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await orderCache.SetAsync(orderQuery, "b", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await userCache.EvictByTagsAsync(["User"])).IsSuccess);

        var container = new RedisOutputCacheContainer(store);
        var cacheTags = await container.GetCacheTagsAsync();
        var cacheTypes = await container.GetCacheTypesAsync();

        Assert.False(cacheTags.ContainsKey("User"));
        Assert.True(cacheTags.ContainsKey("Order"));
        Assert.DoesNotContain(typeof(AppAQuery).FullName!, cacheTypes.Keys);
        Assert.Contains(typeof(AppBQuery).FullName!, cacheTypes.Keys);
        Assert.Null(await container.GetResponseTypeAsync<AppAQuery>());
        Assert.Equal(typeof(string), await container.GetResponseTypeAsync<AppBQuery>());
        Assert.Null(await store.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(userQuery)));
        Assert.Equal(
            JsonConvert.SerializeObject("b"),
            await store.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(orderQuery)));
    }

    [Fact]
    public async Task TwoApps_SameRedis_NoInstanceName_BothCanSetAndGet()
    {
        var shared = new InMemoryDistributedCache();
        var appA = CreateCache<AppAQuery>(shared);
        var appB = CreateCache<AppBQuery>(shared);

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");

        Assert.True((await appA.SetAsync(aQuery, "a-payload", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b-payload", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        var aHit = await appA.GetAsync(aQuery);
        var bHit = await appB.GetAsync(bQuery);
        Assert.True(aHit.IsSuccess);
        Assert.True(bHit.IsSuccess);
        Assert.Equal("a-payload", aHit.Value);
        Assert.Equal("b-payload", bHit.Value);

        var typeMapJson = await shared.GetStringAsync(RequestResponseTypesKey);
        var typeMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(typeMapJson!)!;
        Assert.Contains(typeof(AppAQuery).FullName!, typeMap.Keys);
        Assert.Contains(typeof(AppBQuery).FullName!, typeMap.Keys);
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

        var aMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await shared.GetStringAsync("app-a:" + RequestResponseTypesKey))!)!;
        var bMap = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await shared.GetStringAsync("app-b:" + RequestResponseTypesKey))!)!;

        Assert.Contains(typeof(AppAQuery).FullName!, aMap.Keys);
        Assert.DoesNotContain(typeof(AppBQuery).FullName!, aMap.Keys);
        Assert.Contains(typeof(AppBQuery).FullName!, bMap.Keys);
        Assert.DoesNotContain(typeof(AppAQuery).FullName!, bMap.Keys);
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

        var aQuery = new AppAQuery(1);
        var bQuery = new AppBQuery("x");
        Assert.True((await appA.SetAsync(aQuery, "a", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await appB.SetAsync(bQuery, "b", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        Assert.True((await appA.FlushAll()).IsSuccess);

        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(aQuery)));
        Assert.Null(await shared.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(bQuery)));
        Assert.Null(await shared.GetStringAsync(CacheTagsKey));
        Assert.Null(await shared.GetStringAsync(CacheTypesKey));
        Assert.Null(await shared.GetStringAsync(RequestResponseTypesKey));
    }

    [Fact]
    public async Task Legacy_DictionaryTypeType_Json_DoesNotBlockSetAsync()
    {
        var store = new InMemoryDistributedCache();

        // Mimic pre-1.4.2 serialization of Dictionary<Type, Type> (keys/values are AQN strings in JSON).
        var legacyMap = new Dictionary<Type, Type>
        {
            [typeof(string)] = typeof(int)
        };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(legacyMap));

        // Plus an unresolved foreign assembly entry (string form as left in Redis).
        var seeded = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await store.GetStringAsync(RequestResponseTypesKey))!)!;
        seeded["OtherService.Queries.ForeignQuery, OtherService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"] =
            "OtherService.Dtos.ForeignDto, OtherService, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null";
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(seeded));

        var cache = CreateCache<LocalQuery>(store);
        var query = new LocalQuery(3);
        var set = await cache.SetAsync(query, "ok", tags: ["User"], expirationInSeconds: 30);
        Assert.True(set.IsSuccess);
        Assert.Equal(JsonConvert.SerializeObject("ok"), await store.GetStringAsync(RequestOutputCacheHelper.GetCacheKey(query)));
    }

    [Fact]
    public async Task GetResponseType_ResolvesLegacyAssemblyQualifiedNameKey()
    {
        var store = new InMemoryDistributedCache();
        var requestAqn = typeof(LocalQuery).AssemblyQualifiedName!;
        var responseAqn = typeof(string).AssemblyQualifiedName!;
        var legacy = new Dictionary<string, string> { [requestAqn] = responseAqn };
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(legacy));

        var container = new RedisOutputCacheContainer(store);
        var resolved = await container.GetResponseTypeAsync<LocalQuery>();
        Assert.Equal(typeof(string), resolved);
    }

    [Fact]
    public async Task GetAsync_WithForeignEntries_StillHits()
    {
        var store = new InMemoryDistributedCache();
        var cache = CreateCache<LocalQuery>(store);
        var query = new LocalQuery(9);

        Assert.True((await cache.SetAsync(query, "hit-me", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        var map = JsonConvert.DeserializeObject<Dictionary<string, string>>(
            (await store.GetStringAsync(RequestResponseTypesKey))!)!;
        map["Foreign.App.Query, Foreign"] = "Foreign.App.Dto, Foreign";
        await store.SetStringAsync(RequestResponseTypesKey, JsonConvert.SerializeObject(map));

        var hit = await cache.GetAsync(query);
        Assert.True(hit.IsSuccess);
        Assert.Equal("hit-me", hit.Value);
    }

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

        var containerA = new RedisOutputCacheContainer(new RedisCompareAndSwapIndexStore(coordinated, store, gate));
        var containerB = new RedisOutputCacheContainer(new RedisCompareAndSwapIndexStore(coordinated, store, gate));

        var results = await Task.WhenAll(
            containerA.UpdateContainerAsync<AppAQuery>(tags: ["User"], cacheKey: "a-key", responseType: typeof(string)),
            containerB.UpdateContainerAsync<AppBQuery>(tags: ["Order"], cacheKey: "b-key", responseType: typeof(string)));

        Assert.All(results, result => Assert.True(result.IsSuccess));

        var container = new RedisOutputCacheContainer(store);

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
            var container = new RedisOutputCacheContainer(new RedisCompareAndSwapIndexStore(store, store, gate));
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

        var container = new RedisOutputCacheContainer(store);
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

    private static RedisRequestOutputCache<TRequest, string> CreateCache<TRequest>(IDistributedCache store)
        where TRequest : IRequest<string> =>
        new(
            NullLogger<RedisRequestOutputCache<TRequest, string>>.Instance,
            store,
            new RedisOutputCacheContainer(store));
}
