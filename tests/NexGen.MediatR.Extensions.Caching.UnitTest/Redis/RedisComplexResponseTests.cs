using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using NexGen.MediatR.Extensions.Caching.Redis;
using NexGen.MediatR.Extensions.Caching.Redis.Containers;
using NexGen.MediatR.Extensions.Caching.UnitTest.Helpers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Redis;

public sealed class RedisComplexResponseTests
{
    private sealed record ProductQuery(int Id) : IRequest<ProductDto>;

    private sealed class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public List<string> Tags { get; set; } = [];
    }

    [Fact]
    public async Task SetAsync_ComplexDto_RoundTripsThroughSharedRedis()
    {
        var store = new InMemoryDistributedCache();
        var cache = new RedisRequestOutputCache<ProductQuery, ProductDto>(
            NullLogger<RedisRequestOutputCache<ProductQuery, ProductDto>>.Instance,
            store,
            new RedisOutputCacheContainer(store));

        var query = new ProductQuery(42);
        var dto = new ProductDto
        {
            Id = 42,
            Name = "Widget",
            Price = 19.99m,
            Tags = ["sale", "featured"]
        };

        Assert.True((await cache.SetAsync(query, dto, tags: ["Product"], expirationInSeconds: 120)).IsSuccess);

        var hit = await cache.GetAsync(query);
        Assert.True(hit.IsSuccess);
        Assert.Equal(dto.Id, hit.Value.Id);
        Assert.Equal(dto.Name, hit.Value.Name);
        Assert.Equal(dto.Price, hit.Value.Price);
        Assert.Equal(dto.Tags, hit.Value.Tags);
    }

    [Fact]
    public async Task EvictByTagsAsync_RemovesComplexDtoEntry()
    {
        var store = new InMemoryDistributedCache();
        var cache = new RedisRequestOutputCache<ProductQuery, ProductDto>(
            NullLogger<RedisRequestOutputCache<ProductQuery, ProductDto>>.Instance,
            store,
            new RedisOutputCacheContainer(store));

        var query = new ProductQuery(1);
        Assert.True((await cache.SetAsync(query, new ProductDto { Id = 1, Name = "A" }, tags: ["Product"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await cache.EvictByTagsAsync(["Product"])).IsSuccess);
        Assert.True((await cache.GetAsync(query)).IsFailed);
    }
}
