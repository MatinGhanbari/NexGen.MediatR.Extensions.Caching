using FluentResults;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Behaviors;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Behaviors;

public sealed class RequestOutputCacheHitHeaderTests
{
    [Fact]
    public async Task CacheHit_SetsResponseHeader()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var behavior = new RequestOutputCacheBehavior<CachedQuery, string>(
            new HitCache(),
            new RequestOutputCacheDefaults(),
            accessor);

        var response = await behavior.Handle(
            new CachedQuery(1),
            _ => Task.FromResult("from-handler"),
            CancellationToken.None);

        Assert.Equal("cached", response);
        Assert.Equal(
            RequestCacheConstants.CacheHitResponseHeaderValue,
            httpContext.Response.Headers[RequestCacheConstants.CacheHitResponseHeaderName].ToString());
    }

    [Fact]
    public async Task CacheHit_DoesNotSetHeader_WhenDisabled()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var behavior = new RequestOutputCacheBehavior<CachedQuery, string>(
            new HitCache(),
            new RequestOutputCacheDefaults { EnableCacheHitResponseHeader = false },
            accessor);

        await behavior.Handle(
            new CachedQuery(1),
            _ => Task.FromResult("from-handler"),
            CancellationToken.None);

        Assert.False(httpContext.Response.Headers.ContainsKey(RequestCacheConstants.CacheHitResponseHeaderName));
    }

    [Fact]
    public async Task CacheMiss_DoesNotSetHeader()
    {
        var httpContext = new DefaultHttpContext();
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var behavior = new RequestOutputCacheBehavior<CachedQuery, string>(
            new MissCache(),
            new RequestOutputCacheDefaults(),
            accessor);

        var response = await behavior.Handle(
            new CachedQuery(1),
            _ => Task.FromResult("from-handler"),
            CancellationToken.None);

        Assert.Equal("from-handler", response);
        Assert.False(httpContext.Response.Headers.ContainsKey(RequestCacheConstants.CacheHitResponseHeaderName));
    }

    [Fact]
    public async Task CacheHit_WithoutHttpContext_DoesNotThrow()
    {
        var behavior = new RequestOutputCacheBehavior<CachedQuery, string>(new HitCache());

        var response = await behavior.Handle(
            new CachedQuery(1),
            _ => Task.FromResult("from-handler"),
            CancellationToken.None);

        Assert.Equal("cached", response);
    }

    [Fact]
    public async Task CacheHit_ThroughDi_SetsHeaderOnHttpContext()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheHitHeaderTests>());
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());

        await using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        accessor.HttpContext = httpContext;

        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new DiCachedQuery());
        await mediator.Send(new DiCachedQuery());

        Assert.Equal(
            RequestCacheConstants.CacheHitResponseHeaderValue,
            httpContext.Response.Headers[RequestCacheConstants.CacheHitResponseHeaderName].ToString());
    }

    [Fact]
    public async Task CacheHit_ThroughDi_DoesNotSetHeader_WhenDisabled()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheHitHeaderTests>());
        services.AddMediatROutputCache(opt =>
        {
            opt.UseMemoryCache();
            opt.EnableCacheHitResponseHeader(false);
        });

        await using var provider = services.BuildServiceProvider();
        var accessor = provider.GetRequiredService<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        accessor.HttpContext = httpContext;

        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new DiCachedQuery());
        await mediator.Send(new DiCachedQuery());

        Assert.False(httpContext.Response.Headers.ContainsKey(RequestCacheConstants.CacheHitResponseHeaderName));
    }

    [RequestOutputCache(["tag"])]
    private sealed record CachedQuery(int Id) : IRequest<string>;

    [RequestOutputCache(["header-di"], expirationInSeconds: 60)]
    public sealed record DiCachedQuery : IRequest<string>;

    public sealed class DiCachedQueryHandler : IRequestHandler<DiCachedQuery, string>
    {
        public Task<string> Handle(DiCachedQuery request, CancellationToken cancellationToken)
            => Task.FromResult("di-ok");
    }

    private sealed class HitCache : IRequestOutputCache<CachedQuery, string>
    {
        public Task<Result<string>> GetAsync(CachedQuery request, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok("cached"));

        public Task<Result> SetAsync(
            CachedQuery request,
            string response,
            IEnumerable<string>? tags = null,
            int expirationInSeconds = default,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> FlushAll(CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());
    }

    private sealed class MissCache : IRequestOutputCache<CachedQuery, string>
    {
        public Task<Result<string>> GetAsync(CachedQuery request, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Fail<string>(ErrorMessages.ResponseNotFound));

        public Task<Result> SetAsync(
            CachedQuery request,
            string response,
            IEnumerable<string>? tags = null,
            int expirationInSeconds = default,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> FlushAll(CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());
    }
}
