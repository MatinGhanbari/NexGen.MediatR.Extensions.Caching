using System.Threading.Channels;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Eviction;
using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Eviction;

public sealed class InProcessEvictionBusTests
{
    private sealed record SampleQuery(int Id) : IRequest<string>;

    [Fact]
    public async Task DualDi_InProcessBus_EvictsQueryCacheByTag()
    {
        var bus = new InProcessRequestOutputCacheEvictionBus();

        var queryServices = new ServiceCollection();
        queryServices.AddLogging();
        queryServices.AddMediatROutputCache(options =>
        {
            options.UseMemoryCache();
            options.UseInProcessEvictionBus(bus);
        });

        var commandServices = new ServiceCollection();
        commandServices.AddMediatROutputCacheEviction(options => options.UseInProcessEvictionBus(bus));

        await using var queryProvider = queryServices.BuildServiceProvider();
        await using var commandProvider = commandServices.BuildServiceProvider();

        var hosted = queryProvider.GetServices<IHostedService>()
            .OfType<RequestOutputCacheEvictionHostedService>()
            .Single();

        using var cts = new CancellationTokenSource();
        await hosted.StartAsync(cts.Token);

        var query = new SampleQuery(42);
        await using (var scope = queryProvider.CreateAsyncScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<IRequestOutputCache<SampleQuery, string>>();
            var set = await cache.SetAsync(query, "cached", tags: ["User"], expirationInSeconds: 60);
            Assert.True(set.IsSuccess);

            var hit = await cache.GetAsync(query);
            Assert.True(hit.IsSuccess);
            Assert.Equal("cached", hit.Value);
        }

        var publisher = commandProvider.GetRequiredService<IRequestOutputCacheEvictionPublisher>();
        await publisher.PublishAsync(new RequestOutputCacheEvictionMessage { Tags = ["User"] });

        await WaitForAsync(async () =>
        {
            await using var scope = queryProvider.CreateAsyncScope();
            var cache = scope.ServiceProvider.GetRequiredService<IRequestOutputCache<SampleQuery, string>>();
            var result = await cache.GetAsync(query);
            return result.IsFailed;
        }, TimeSpan.FromSeconds(2));

        await cts.CancelAsync();
        await hosted.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task CustomBus_PublisherAndSubscriber_EvictsTags()
    {
        var channel = Channel.CreateUnbounded<RequestOutputCacheEvictionMessage>();
        var publisher = new FakeBusPublisher(channel.Writer);
        var subscriber = new FakeBusSubscriber(channel.Reader);

        var queryServices = new ServiceCollection();
        queryServices.AddLogging();
        queryServices.AddMediatROutputCache(options =>
        {
            options.UseMemoryCache();
            options.UseCustomEvictionPublisher(publisher);
            options.UseCustomEvictionSubscriber(subscriber);
        });

        var commandServices = new ServiceCollection();
        commandServices.AddMediatROutputCacheEviction(options =>
            options.UseCustomEvictionPublisher(publisher));

        await using var queryProvider = queryServices.BuildServiceProvider();
        await using var commandProvider = commandServices.BuildServiceProvider();

        var hosted = queryProvider.GetServices<IHostedService>()
            .OfType<RequestOutputCacheEvictionHostedService>()
            .Single();

        using var cts = new CancellationTokenSource();
        await hosted.StartAsync(cts.Token);

        var query = new SampleQuery(7);
        await using (var scope = queryProvider.CreateAsyncScope())
        {
            var cache = scope.ServiceProvider.GetRequiredService<IRequestOutputCache<SampleQuery, string>>();
            Assert.True((await cache.SetAsync(query, "value", tags: ["Order"], expirationInSeconds: 60)).IsSuccess);
        }

        await commandProvider.GetRequiredService<IRequestOutputCacheEvictionPublisher>()
            .PublishAsync(new RequestOutputCacheEvictionMessage { Tags = ["Order"] });

        await WaitForAsync(async () =>
        {
            await using var scope = queryProvider.CreateAsyncScope();
            var cache = scope.ServiceProvider.GetRequiredService<IRequestOutputCache<SampleQuery, string>>();
            return (await cache.GetAsync(query)).IsFailed;
        }, TimeSpan.FromSeconds(2));

        await cts.CancelAsync();
        await hosted.StopAsync(CancellationToken.None);
    }

    private static async Task WaitForAsync(Func<Task<bool>> condition, TimeSpan timeout)
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (await condition())
                return;

            await Task.Delay(25);
        }

        Assert.Fail("Condition was not met before timeout.");
    }

    private sealed class FakeBusPublisher(ChannelWriter<RequestOutputCacheEvictionMessage> writer)
        : IRequestOutputCacheEvictionPublisher
    {
        public async Task PublishAsync(
            RequestOutputCacheEvictionMessage message,
            CancellationToken cancellationToken = default)
        {
            await writer.WriteAsync(message, cancellationToken);
        }
    }

    private sealed class FakeBusSubscriber(ChannelReader<RequestOutputCacheEvictionMessage> reader)
        : IRequestOutputCacheEvictionSubscriber
    {
        public async Task SubscribeAsync(
            Func<RequestOutputCacheEvictionMessage, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            await foreach (var message in reader.ReadAllAsync(cancellationToken))
            {
                await handler(message, cancellationToken);
            }
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

    [Fact]
    public async Task EvictAttribute_PublishesTagsOnSuccess()
    {
        var published = new List<string[]>();
        var publisher = new CapturingPublisher(published);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<RequestOutputCacheEvictBehaviorTests>());
        services.AddTransient<IRequestHandler<EvictCommand, Unit>, EvictCommandHandler>();
        services.AddMediatROutputCacheEviction(options => options.UseCustomEvictionPublisher(publisher));

        await using var provider = services.BuildServiceProvider();
        var mediator = provider.GetRequiredService<IMediator>();

        await mediator.Send(new EvictCommand());

        Assert.Single(published);
        Assert.Equal(["User"], published[0]);
    }

    private sealed class CapturingPublisher(List<string[]> sink) : IRequestOutputCacheEvictionPublisher
    {
        public Task PublishAsync(
            RequestOutputCacheEvictionMessage message,
            CancellationToken cancellationToken = default)
        {
            sink.Add(message.Tags);
            return Task.CompletedTask;
        }
    }
}
