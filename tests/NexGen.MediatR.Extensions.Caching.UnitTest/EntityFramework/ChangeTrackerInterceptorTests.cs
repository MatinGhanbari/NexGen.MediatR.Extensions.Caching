using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.EntityFramework;
using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.EntityFramework;

public sealed class ChangeTrackerInterceptorTests
{
    [Fact]
    public async Task SavedChangesAsync_AddedEntity_PublishesEntityNameTag()
    {
        var published = new List<string[]>();
        var publisher = new CapturingPublisher(published);
        await using var provider = BuildProvider(publisher: publisher);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();
        db.Users.Add(new UserEntity { Name = "Alice" });
        await db.SaveChangesAsync();

        Assert.Single(published);
        Assert.Equal(["UserEntity"], published[0]);
    }

    [Fact]
    public async Task SavedChangesAsync_ModifiedAndDeleted_CollectDistinctEntityTags()
    {
        var published = new List<string[]>();
        var publisher = new CapturingPublisher(published);
        await using var provider = BuildProvider(publisher: publisher);

        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<TestDbContext>();

        var user = new UserEntity { Name = "Bob" };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        published.Clear();

        user.Name = "Robert";
        db.Orders.Add(new OrderEntity { UserId = user.Id, Amount = 10 });
        db.Users.Remove(user);
        await db.SaveChangesAsync();

        Assert.Single(published);
        Assert.Contains("UserEntity", published[0]);
        Assert.Contains("OrderEntity", published[0]);
        Assert.Equal(2, published[0].Length);
    }

    [Fact]
    public async Task SavedChangesAsync_WithoutPublisher_EvictsViaLocalInvalidator()
    {
        await using var provider = BuildProvider(publisher: null, registerCache: true);

        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<TestDbContext>();
        var cache = sp.GetRequiredService<IRequestOutputCache<CachedUserQuery, string>>();

        Assert.True((await cache.SetAsync(new CachedUserQuery(1), "cached", tags: ["UserEntity"], expirationInSeconds: 60)).IsSuccess);

        db.Users.Add(new UserEntity { Name = "Eve" });
        await db.SaveChangesAsync();

        Assert.True((await cache.GetAsync(new CachedUserQuery(1))).IsFailed);
    }

    [Fact]
    public async Task SaveChangesFailed_DoesNotPublishOrEvict()
    {
        var published = new List<string[]>();
        var publisher = new CapturingPublisher(published);
        await using var provider = BuildProvider(publisher: publisher, registerCache: true);

        await using var scope = provider.CreateAsyncScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<TestDbContext>();
        var cache = sp.GetRequiredService<IRequestOutputCache<CachedUserQuery, string>>();

        Assert.True((await cache.SetAsync(new CachedUserQuery(1), "cached", tags: ["UserEntity"], expirationInSeconds: 60)).IsSuccess);

        db.Users.Add(new UserEntity { Name = "Fail" });
        db.FailNextSave = true;

        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());

        Assert.Empty(published);
        Assert.Equal("cached", (await cache.GetAsync(new CachedUserQuery(1))).Value);
    }

    private static ServiceProvider BuildProvider(
        CapturingPublisher? publisher,
        bool registerCache = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        if (registerCache)
            services.AddMediatROutputCache(opt => opt.UseMemoryCache());

        if (publisher is not null)
            services.AddSingleton<IRequestOutputCacheEvictionPublisher>(publisher);

        services.AddDbContext<TestDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase(Guid.NewGuid().ToString());
            options.AddInterceptors(new ChangeTrackerInterceptor(sp));
        });

        return services.BuildServiceProvider();
    }

    private sealed class CapturingPublisher(List<string[]> sink) : IRequestOutputCacheEvictionPublisher
    {
        public Task PublishAsync(RequestOutputCacheEvictionMessage message, CancellationToken cancellationToken = default)
        {
            sink.Add(message.Tags);
            return Task.CompletedTask;
        }
    }

    private sealed class UserEntity
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class OrderEntity
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
    }

    private sealed record CachedUserQuery(int Id) : IRequest<string>;

    private sealed class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<UserEntity> Users => Set<UserEntity>();
        public DbSet<OrderEntity> Orders => Set<OrderEntity>();

        public bool FailNextSave { get; set; }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            if (FailNextSave)
            {
                FailNextSave = false;
                throw new InvalidOperationException("Simulated save failure.");
            }

            return await base.SaveChangesAsync(cancellationToken);
        }
    }
}
