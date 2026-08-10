using FluentResults;
using MediatR;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Behaviors;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Garnet.Configurations;
using NexGen.MediatR.Extensions.Caching.Redis.Configurations;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Configurations;

public sealed class ProviderSpecificCacheOptionsTests
{
    [Fact]
    public void UseMemoryCache_WithDefaultExpiration_RegistersDefaults()
    {
        var services = new ServiceCollection();
        services.AddMediatROutputCache(opt =>
            opt.UseMemoryCache(o => o.DefaultExpirationInSeconds = 600));

        using var provider = services.BuildServiceProvider();
        var defaults = provider.GetRequiredService<RequestOutputCacheDefaults>();

        Assert.Equal(600, defaults.DefaultExpirationInSeconds);
    }

    [Fact]
    public void UseRedisCache_ActionOverload_AppliesInstanceNameAndDatabase()
    {
        var services = new ServiceCollection();
        services.AddMediatROutputCache(opt =>
            opt.UseRedisCache(o =>
            {
                o.ConnectionString = "localhost:6379";
                o.InstanceName = "my-app:";
                o.Database = 2;
                o.DefaultExpirationInSeconds = 120;
            }));

        using var provider = services.BuildServiceProvider();
        var redisOptions = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;
        var defaults = provider.GetRequiredService<RequestOutputCacheDefaults>();

        Assert.Equal("my-app:", redisOptions.InstanceName);
        Assert.NotNull(redisOptions.ConfigurationOptions);
        Assert.Equal(2, redisOptions.ConfigurationOptions.DefaultDatabase);
        Assert.Equal(120, defaults.DefaultExpirationInSeconds);
    }

    [Fact]
    public void UseGarnetCache_ActionOverload_AppliesInstanceNameAndDatabase()
    {
        var services = new ServiceCollection();
        services.AddMediatROutputCache(opt =>
            opt.UseGarnetCache(o =>
            {
                o.ConnectionString = "localhost:6379";
                o.InstanceName = "garnet:";
                o.Database = 3;
            }));

        using var provider = services.BuildServiceProvider();
        var redisOptions = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

        Assert.Equal("garnet:", redisOptions.InstanceName);
        Assert.NotNull(redisOptions.ConfigurationOptions);
        Assert.Equal(3, redisOptions.ConfigurationOptions.DefaultDatabase);
    }

    [Fact]
    public void UseRedisCache_StringOverload_StillRegistersProvider()
    {
        var services = new ServiceCollection();
        services.AddMediatROutputCache(opt => opt.UseRedisCache("localhost:6379"));

        using var provider = services.BuildServiceProvider();
        var redisOptions = provider.GetRequiredService<IOptions<RedisCacheOptions>>().Value;

        Assert.Equal("localhost:6379", redisOptions.Configuration);
        Assert.Null(redisOptions.ConfigurationOptions);
    }

    [Fact]
    public async Task Behavior_UsesProviderDefault_WhenAttributeUsesLibraryConstant()
    {
        var cache = new CapturingCache<CachedQuery>();
        var behavior = new RequestOutputCacheBehavior<CachedQuery, string>(
            cache,
            new RequestOutputCacheDefaults { DefaultExpirationInSeconds = 90 });

        var response = await behavior.Handle(
            new CachedQuery(1),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal("ok", response);
        Assert.Equal(90, cache.LastExpirationInSeconds);
    }

    [Fact]
    public async Task Behavior_KeepsExplicitAttributeExpiration()
    {
        var cache = new CapturingCache<ExplicitExpirationQuery>();
        var behavior = new RequestOutputCacheBehavior<ExplicitExpirationQuery, string>(
            cache,
            new RequestOutputCacheDefaults { DefaultExpirationInSeconds = 90 });

        await behavior.Handle(
            new ExplicitExpirationQuery(1),
            _ => Task.FromResult("ok"),
            CancellationToken.None);

        Assert.Equal(15, cache.LastExpirationInSeconds);
    }

    [RequestOutputCache(["tag"])]
    private sealed record CachedQuery(int Id) : IRequest<string>;

    [RequestOutputCache(["tag"], expirationInSeconds: 15)]
    private sealed record ExplicitExpirationQuery(int Id) : IRequest<string>;

    private sealed class CapturingCache<TRequest> : IRequestOutputCache<TRequest, string>
        where TRequest : IRequest<string>
    {
        public int? LastExpirationInSeconds { get; private set; }

        public Task<Result<string>> GetAsync(TRequest request, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Fail<string>(ErrorMessages.ResponseNotFound));

        public Task<Result> SetAsync(
            TRequest request,
            string response,
            IEnumerable<string>? tags = null,
            int expirationInSeconds = default,
            CancellationToken cancellationToken = default)
        {
            LastExpirationInSeconds = expirationInSeconds;
            return Task.FromResult(Result.Ok());
        }

        public Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> FlushAll(CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());
    }
}
