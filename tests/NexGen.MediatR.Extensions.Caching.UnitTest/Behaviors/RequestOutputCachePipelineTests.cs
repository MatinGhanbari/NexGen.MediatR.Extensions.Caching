using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Behaviors;

public sealed class RequestOutputCachePipelineTests
{
    private static int _handlerInvocationCount;

    [RequestOutputCache(["User"], expirationInSeconds: 60)]
    public sealed record CachedUsersQuery : IRequest<string>;

    public sealed class CachedUsersQueryHandler : IRequestHandler<CachedUsersQuery, string>
    {
        public Task<string> Handle(CachedUsersQuery request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _handlerInvocationCount);
            return Task.FromResult($"users-{_handlerInvocationCount}");
        }
    }

    public sealed record UncachedQuery : IRequest<string>;

    public sealed class UncachedQueryHandler : IRequestHandler<UncachedQuery, string>
    {
        public Task<string> Handle(UncachedQuery request, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _handlerInvocationCount);
            return Task.FromResult("uncached");
        }
    }

    [RequestOutputCacheEvict("User")]
    public sealed record EvictUsersCommand : IRequest<Unit>;

    public sealed class EvictUsersCommandHandler : IRequestHandler<EvictUsersCommand, Unit>
    {
        public Task<Unit> Handle(EvictUsersCommand request, CancellationToken cancellationToken)
            => Task.FromResult(Unit.Value);
    }

    [Fact]
    public async Task CachedQuery_SecondSend_ReturnsCachedWithoutReExecutingHandler()
    {
        _handlerInvocationCount = 0;
        await using var provider = BuildQueryProvider();

        var mediator = provider.GetRequiredService<IMediator>();

        var first = await mediator.Send(new CachedUsersQuery());
        var second = await mediator.Send(new CachedUsersQuery());

        Assert.Equal(first, second);
        Assert.Equal(1, _handlerInvocationCount);
    }

    [Fact]
    public async Task UncachedQuery_AlwaysExecutesHandler()
    {
        _handlerInvocationCount = 0;
        await using var provider = BuildQueryProvider();

        var mediator = provider.GetRequiredService<IMediator>();
        await mediator.Send(new UncachedQuery());
        await mediator.Send(new UncachedQuery());

        Assert.Equal(2, _handlerInvocationCount);
    }

    [Fact]
    public async Task EvictCommand_WithLocalInvalidator_ClearsCachedQuery()
    {
        _handlerInvocationCount = 0;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCachePipelineTests>());
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        var cached = await mediator.Send(new CachedUsersQuery());
        Assert.Equal(1, _handlerInvocationCount);

        await mediator.Send(new EvictUsersCommand());

        var afterEvict = await mediator.Send(new CachedUsersQuery());
        Assert.NotEqual(cached, afterEvict);
        Assert.Equal(2, _handlerInvocationCount);
    }

    private static ServiceProvider BuildQueryProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCachePipelineTests>());
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());
        return services.BuildServiceProvider();
    }
}
