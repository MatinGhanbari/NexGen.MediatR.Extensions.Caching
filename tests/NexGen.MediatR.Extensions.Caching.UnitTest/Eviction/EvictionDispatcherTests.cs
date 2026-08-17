using FluentResults;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;
using NexGen.MediatR.Extensions.Caching.Helpers;
using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Eviction;

public sealed class RequestOutputCacheEvictionDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_NormalizesTags_AndEvictsOnce()
    {
        var invalidator = new CapturingInvalidator();
        var notifier = new CapturingNotifier();
        var dispatcher = CreateDispatcher(invalidator, notifier);

        var result = await dispatcher.DispatchAsync([" User ", "User", "", "Order", "Order"]);

        Assert.True(result.IsSuccess);
        Assert.Single(invalidator.Calls);
        Assert.Equal(["User", "Order"], invalidator.Calls[0]);
        Assert.Single(notifier.Calls);
        Assert.Equal(["User", "Order"], notifier.Calls[0]);
    }

    [Fact]
    public async Task DispatchAsync_EmptyTags_DoesNotEvictOrNotify()
    {
        var invalidator = new CapturingInvalidator();
        var notifier = new CapturingNotifier();
        var dispatcher = CreateDispatcher(invalidator, notifier);

        var result = await dispatcher.DispatchAsync(["  ", null!]);

        Assert.True(result.IsSuccess);
        Assert.Empty(invalidator.Calls);
        Assert.Empty(notifier.Calls);
    }

    [Fact]
    public async Task DispatchAsync_WithoutNotifier_EvictsLocallyOnly()
    {
        var invalidator = new CapturingInvalidator();
        var dispatcher = CreateDispatcher(invalidator, notifier: null);

        var result = await dispatcher.DispatchAsync(["User"]);

        Assert.True(result.IsSuccess);
        Assert.Equal(["User"], invalidator.Calls.Single());
    }

    [Fact]
    public async Task DispatchAsync_WhenLocalEvictFails_DoesNotNotify()
    {
        var invalidator = new CapturingInvalidator { Fail = true };
        var notifier = new CapturingNotifier();
        var dispatcher = CreateDispatcher(invalidator, notifier);

        var result = await dispatcher.DispatchAsync(["User"]);

        Assert.True(result.IsFailed);
        Assert.Empty(notifier.Calls);
    }

    [Fact]
    public void NotificationFormatter_RoundTripsAndRejectsMalformedPayload()
    {
        var payload = RequestOutputCacheEvictionNotificationFormatter.Serialize(
            new RequestOutputCacheEvictionNotification
            {
                Tags = ["User", "Order"],
                SenderId = "abc",
                TimestampUnixMs = 123
            });

        var parsed = RequestOutputCacheEvictionNotificationFormatter.TryDeserialize(payload);
        Assert.NotNull(parsed);
        Assert.Equal(["User", "Order"], parsed!.Tags);
        Assert.Equal("abc", parsed.SenderId);
        Assert.Equal(123, parsed.TimestampUnixMs);

        Assert.Null(RequestOutputCacheEvictionNotificationFormatter.TryDeserialize("{not-json"));
        Assert.Null(RequestOutputCacheEvictionNotificationFormatter.TryDeserialize("""{"Tags":[]}"""));
        Assert.Null(RequestOutputCacheEvictionNotificationFormatter.TryDeserialize(null));
    }

    [Fact]
    public void EvictionChannel_PrefixesInstanceName()
    {
        Assert.Equal(
            "NexGen.MediatR.Extensions.Caching:Evict",
            RequestOutputCacheEvictionChannel.Resolve(null, null));

        Assert.Equal(
            "my-app:NexGen.MediatR.Extensions.Caching:Evict",
            RequestOutputCacheEvictionChannel.Resolve("my-app:", null));

        Assert.Equal(
            "my-app:custom",
            RequestOutputCacheEvictionChannel.Resolve("my-app", "custom"));
    }

    private static RequestOutputCacheEvictionDispatcher CreateDispatcher(
        IRequestOutputCacheInvalidator invalidator,
        IRequestOutputCacheEvictionNotifier? notifier)
    {
        var services = new ServiceCollection();
        services.AddSingleton(invalidator);
        if (notifier is not null)
            services.AddSingleton(notifier);

        return new RequestOutputCacheEvictionDispatcher(invalidator, services.BuildServiceProvider());
    }

    private sealed class CapturingInvalidator : IRequestOutputCacheInvalidator
    {
        public bool Fail { get; set; }
        public List<string[]> Calls { get; } = [];

        public Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
        {
            Calls.Add(tags.ToArray());
            return Task.FromResult(Fail ? Result.Fail("failed") : Result.Ok());
        }

        public Task<Result> FlushAll(CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());
    }

    private sealed class CapturingNotifier : IRequestOutputCacheEvictionNotifier
    {
        public List<string[]> Calls { get; } = [];

        public Task NotifyAsync(IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
        {
            Calls.Add(tags as string[] ?? tags.ToArray());
            return Task.CompletedTask;
        }
    }
}

public sealed class RequestOutputCacheEvictBehaviorTests
{
    [RequestOutputCacheEvict("User")]
    private sealed record EvictCommand : IRequest<Unit>;

    private sealed class EvictCommandHandler : IRequestHandler<EvictCommand, Unit>
    {
        public Task<Unit> Handle(EvictCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Unit.Value);
    }

    [RequestOutputCacheEvict("User", "Order", "User", " ", "Order")]
    private sealed record MultiTagEvictCommand : IRequest<Unit>;

    private sealed class MultiTagEvictCommandHandler : IRequestHandler<MultiTagEvictCommand, Unit>
    {
        public Task<Unit> Handle(MultiTagEvictCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Unit.Value);
    }

    [RequestOutputCacheEvict("User")]
    private sealed record FailedResultCommand : IRequest<Result>;

    private sealed class FailedResultCommandHandler : IRequestHandler<FailedResultCommand, Result>
    {
        public Task<Result> Handle(FailedResultCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Fail("handler failed"));
    }

    [RequestOutputCacheEvict("User")]
    private sealed record SuccessfulResultCommand : IRequest<Result>;

    private sealed class SuccessfulResultCommandHandler : IRequestHandler<SuccessfulResultCommand, Result>
    {
        public Task<Result> Handle(SuccessfulResultCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Result.Ok());
    }

    private sealed record SampleCachedQuery(int Id) : IRequest<string>;

    [Fact]
    public async Task EvictAttribute_LocalInvalidator_EvictsTags()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheEvictBehaviorTests>());
        services.AddTransient<IRequestHandler<EvictCommand, Unit>, EvictCommandHandler>();

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IRequestOutputCache<SampleCachedQuery, string>>();
        var query = new SampleCachedQuery(1);
        Assert.True((await cache.SetAsync(query, "cached", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new EvictCommand());

        Assert.True((await cache.GetAsync(query)).IsFailed);
    }

    [Fact]
    public async Task EvictAttribute_MultipleTags_EvictsAllInOneDispatch()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheEvictBehaviorTests>());
        services.AddTransient<IRequestHandler<MultiTagEvictCommand, Unit>, MultiTagEvictCommandHandler>();

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IRequestOutputCache<SampleCachedQuery, string>>();
        Assert.True((await cache.SetAsync(new SampleCachedQuery(1), "user", tags: ["User"], expirationInSeconds: 60)).IsSuccess);
        Assert.True((await cache.SetAsync(new SampleCachedQuery(2), "order", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);

        await provider.GetRequiredService<IMediator>().Send(new MultiTagEvictCommand());

        Assert.True((await cache.GetAsync(new SampleCachedQuery(1))).IsFailed);
        Assert.True((await cache.GetAsync(new SampleCachedQuery(2))).IsFailed);
    }

    [Fact]
    public async Task EvictAttribute_FailedFluentResult_DoesNotEvict()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheEvictBehaviorTests>());
        services.AddTransient<IRequestHandler<FailedResultCommand, Result>, FailedResultCommandHandler>();

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IRequestOutputCache<SampleCachedQuery, string>>();
        var query = new SampleCachedQuery(1);
        Assert.True((await cache.SetAsync(query, "cached", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        var result = await provider.GetRequiredService<IMediator>().Send(new FailedResultCommand());

        Assert.True(result.IsFailed);
        Assert.Equal("cached", (await cache.GetAsync(query)).Value);
    }

    [Fact]
    public async Task EvictAttribute_SuccessfulFluentResult_EvictsTags()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheEvictBehaviorTests>());
        services.AddTransient<IRequestHandler<SuccessfulResultCommand, Result>, SuccessfulResultCommandHandler>();

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IRequestOutputCache<SampleCachedQuery, string>>();
        var query = new SampleCachedQuery(1);
        Assert.True((await cache.SetAsync(query, "cached", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        var result = await provider.GetRequiredService<IMediator>().Send(new SuccessfulResultCommand());

        Assert.True(result.IsSuccess);
        Assert.True((await cache.GetAsync(query)).IsFailed);
    }

    [Fact]
    public async Task EvictAttribute_WithNotifier_EvictsLocallyAndNotifies()
    {
        var notifier = new CapturingNotifier();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IRequestOutputCacheEvictionNotifier>(notifier);
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheEvictBehaviorTests>());
        services.AddTransient<IRequestHandler<EvictCommand, Unit>, EvictCommandHandler>();

        await using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<IRequestOutputCache<SampleCachedQuery, string>>();
        var query = new SampleCachedQuery(1);
        Assert.True((await cache.SetAsync(query, "cached", tags: ["User"], expirationInSeconds: 60)).IsSuccess);

        await provider.GetRequiredService<IMediator>().Send(new EvictCommand());

        Assert.True((await cache.GetAsync(query)).IsFailed);
        Assert.Equal(["User"], notifier.Calls.Single());
    }

    [Fact]
    public void EvictAttribute_NormalizesConstructorTags()
    {
        var attribute = new RequestOutputCacheEvictAttribute(
            " User ", "User", "", "Order");

        Assert.Equal(["User", "Order"], attribute.Tags);
    }

    private sealed class CapturingNotifier : IRequestOutputCacheEvictionNotifier
    {
        public List<string[]> Calls { get; } = [];

        public Task NotifyAsync(IReadOnlyCollection<string> tags, CancellationToken cancellationToken = default)
        {
            Calls.Add(tags as string[] ?? tags.ToArray());
            return Task.CompletedTask;
        }
    }
}
