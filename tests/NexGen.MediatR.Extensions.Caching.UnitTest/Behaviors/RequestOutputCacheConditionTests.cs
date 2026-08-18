using FluentResults;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Helpers;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Behaviors;

public sealed class RequestOutputCacheConditionTests
{
    private static int _itemsInvocations;
    private static int _fluentInvocations;
    private static int _pocoInvocations;
    private static int _plainInvocations;

    [Fact]
    public static async Task CacheWhen_False_DoesNotCache()
    {
        _itemsInvocations = 0;
        await using var provider = BuildProvider(opt =>
            opt.CacheWhen<ItemsQuery, ItemsResponse>(x => x.Items.Count > 0));

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new ItemsQuery(IncludeItems: false);

        await mediator.Send(request);
        await mediator.Send(request);

        Assert.Equal(2, _itemsInvocations);
    }

    [Fact]
    public static async Task CacheWhen_True_CachesResponse()
    {
        _itemsInvocations = 0;
        await using var provider = BuildProvider(opt =>
            opt.CacheWhen<ItemsQuery, ItemsResponse>(x => x.Items.Count > 0));

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new ItemsQuery(IncludeItems: true);

        var first = await mediator.Send(request);
        var second = await mediator.Send(request);

        Assert.Equal(first.Items, second.Items);
        Assert.Equal(1, _itemsInvocations);
    }

    [Fact]
    public static async Task CacheWhen_WithRequest_UsesRequestAndResponse()
    {
        _itemsInvocations = 0;
        await using var provider = BuildProvider(opt =>
            opt.CacheWhen<ItemsQuery, ItemsResponse>((req, res) => req.IncludeItems && res.Items.Count > 0));

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new ItemsQuery(IncludeItems: true);

        await mediator.Send(request);
        await mediator.Send(request);

        Assert.Equal(1, _itemsInvocations);
    }

    [Fact]
    public static async Task FluentResult_Failure_IsNotCached()
    {
        _fluentInvocations = 0;
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new FluentResultQuery(Success: false);

        var first = await mediator.Send(request);
        var second = await mediator.Send(request);

        Assert.True(first.IsFailed);
        Assert.True(second.IsFailed);
        Assert.Equal(2, _fluentInvocations);
    }

    [Fact]
    public static async Task FluentResult_Success_IsCached()
    {
        _fluentInvocations = 0;
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new FluentResultQuery(Success: true);

        var first = await mediator.Send(request);
        var second = await mediator.Send(request);

        Assert.True(first.IsSuccess);
        Assert.Equal(first.Value, second.Value);
        Assert.Equal(1, _fluentInvocations);
    }

    [Fact]
    public static async Task Poco_IsSuccessFalse_IsNotCached()
    {
        _pocoInvocations = 0;
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new PocoQuery(IsSuccess: false);

        await mediator.Send(request);
        await mediator.Send(request);

        Assert.Equal(2, _pocoInvocations);
    }

    [Fact]
    public static async Task Poco_IsSuccessTrue_IsCached()
    {
        _pocoInvocations = 0;
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new PocoQuery(IsSuccess: true);

        var first = await mediator.Send(request);
        var second = await mediator.Send(request);

        Assert.Equal(first.Value, second.Value);
        Assert.Equal(1, _pocoInvocations);
    }

    [Fact]
    public static async Task ResponseWithoutIsSuccess_IsCached()
    {
        _plainInvocations = 0;
        await using var provider = BuildProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var first = await mediator.Send(new PlainQuery());
        var second = await mediator.Send(new PlainQuery());

        Assert.Equal(first.Value, second.Value);
        Assert.Equal(1, _plainInvocations);
    }

    [Fact]
    public static async Task CacheUnsuccessfulResponses_CachesFluentResultFailure()
    {
        _fluentInvocations = 0;
        await using var provider = BuildProvider(opt => opt.CacheUnsuccessfulResponses(true));

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new FluentResultQuery(Success: false);

        await mediator.Send(request);
        await mediator.Send(request);

        Assert.Equal(1, _fluentInvocations);
    }

    [Fact]
    public static async Task CacheWhen_TakesPriorityOverIsSuccess()
    {
        _fluentInvocations = 0;
        await using var provider = BuildProvider(opt =>
            opt.CacheWhen<FluentResultQuery, Result<string>>(_ => true));

        var mediator = provider.GetRequiredService<IMediator>();
        var request = new FluentResultQuery(Success: false);

        await mediator.Send(request);
        await mediator.Send(request);

        Assert.Equal(1, _fluentInvocations);
    }

    [Fact]
    public static void Evaluator_NullResponse_IsNotCached()
    {
        Assert.False(RequestOutputCacheResponseEvaluator.ShouldCache<string?>(null, cacheUnsuccessfulResponses: false));
        Assert.False(RequestOutputCacheResponseEvaluator.ShouldCache<string?>(null, cacheUnsuccessfulResponses: true));
    }

    [Fact]
    public static void Evaluator_NonBoolIsSuccess_IsCached()
    {
        var response = new NonBoolSuccessResponse("yes");
        Assert.True(RequestOutputCacheResponseEvaluator.ShouldCache(response, cacheUnsuccessfulResponses: false));
    }

    private static ServiceProvider BuildProvider(Action<RequestOutputCacheConfigurationOption>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheConditionTests>());
        services.AddMediatROutputCache(opt =>
        {
            opt.UseMemoryCache();
            configure?.Invoke(opt);
        });
        return services.BuildServiceProvider();
    }

    [RequestOutputCache(["items"], expirationInSeconds: 60)]
    public sealed record ItemsQuery(bool IncludeItems) : IRequest<ItemsResponse>;

    public sealed record ItemsResponse(IReadOnlyList<string> Items);

    public sealed class ItemsQueryHandler : IRequestHandler<ItemsQuery, ItemsResponse>
    {
        public Task<ItemsResponse> Handle(ItemsQuery request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _itemsInvocations);
            IReadOnlyList<string> items = request.IncludeItems ? ["a"] : [];
            return Task.FromResult(new ItemsResponse(items));
        }
    }

    [RequestOutputCache(["fluent"], expirationInSeconds: 60)]
    public sealed record FluentResultQuery(bool Success) : IRequest<Result<string>>;

    public sealed class FluentResultQueryHandler : IRequestHandler<FluentResultQuery, Result<string>>
    {
        public Task<Result<string>> Handle(FluentResultQuery request, CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref _fluentInvocations);
            return Task.FromResult(request.Success
                ? Result.Ok($"ok-{n}")
                : Result.Fail<string>("fail"));
        }
    }

    [RequestOutputCache(["poco"], expirationInSeconds: 60)]
    public sealed record PocoQuery(bool IsSuccess) : IRequest<PocoResponse>;

    public sealed record PocoResponse(bool IsSuccess, string Value);

    public sealed class PocoQueryHandler : IRequestHandler<PocoQuery, PocoResponse>
    {
        public Task<PocoResponse> Handle(PocoQuery request, CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref _pocoInvocations);
            return Task.FromResult(new PocoResponse(request.IsSuccess, $"v-{n}"));
        }
    }

    [RequestOutputCache(["plain"], expirationInSeconds: 60)]
    public sealed record PlainQuery : IRequest<PlainResponse>;

    public sealed record PlainResponse(string Value);

    public sealed class PlainQueryHandler : IRequestHandler<PlainQuery, PlainResponse>
    {
        public Task<PlainResponse> Handle(PlainQuery request, CancellationToken cancellationToken)
        {
            var n = Interlocked.Increment(ref _plainInvocations);
            return Task.FromResult(new PlainResponse($"plain-{n}"));
        }
    }

    public sealed record NonBoolSuccessResponse(string IsSuccess);
}
