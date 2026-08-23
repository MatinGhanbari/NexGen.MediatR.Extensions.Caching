using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Garnet.Configurations;
using NexGen.MediatR.Extensions.Caching.Redis.Configurations;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Configurations;

public sealed class RequestOutputCacheStartupValidationTests
{
    [Fact]
    public async Task HostStartup_FailsWhenCacheProviderIsMissing()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddMediatROutputCache(_ => { }))
            .Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains(ErrorMessages.CacheProviderNotSelected, exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task HostStartup_FailsWhenMemoryDefaultExpirationIsInvalid(int expirationInSeconds)
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddMediatROutputCache(opt =>
                opt.UseMemoryCache(o => o.DefaultExpirationInSeconds = expirationInSeconds)))
            .Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains(ErrorMessages.InvalidDefaultExpirationInSeconds, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HostStartup_SucceedsForValidMemoryConfiguration()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddMediatROutputCache(opt => opt.UseMemoryCache()))
            .Build();

        await host.StartAsync();
    }

    [Fact]
    public async Task HostStartup_FailsWhenRedisDatabaseIsInvalid()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddMediatROutputCache(opt =>
                opt.UseRedisCache(o =>
                {
                    o.ConnectionString = "localhost:6379";
                    o.Database = -1;
                    o.EnableDistributedEviction = false;
                })))
            .Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains(ErrorMessages.InvalidDatabase, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HostStartup_FailsWhenRedisDefaultExpirationIsInvalid()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddMediatROutputCache(opt =>
                opt.UseRedisCache(o =>
                {
                    o.ConnectionString = "localhost:6379";
                    o.DefaultExpirationInSeconds = -5;
                    o.EnableDistributedEviction = false;
                })))
            .Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains(ErrorMessages.InvalidDefaultExpirationInSeconds, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HostStartup_FailsWhenGarnetDatabaseIsInvalid()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddMediatROutputCache(opt =>
                opt.UseGarnetCache(o =>
                {
                    o.ConnectionString = "localhost:6379";
                    o.Database = -2;
                    o.EnableDistributedEviction = false;
                })))
            .Build();

        var exception = await Assert.ThrowsAsync<OptionsValidationException>(() => host.StartAsync());

        Assert.Contains(ErrorMessages.InvalidDatabase, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HostStartup_SucceedsForValidRedisConfiguration()
    {
        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services => services.AddMediatROutputCache(opt =>
                opt.UseRedisCache(o =>
                {
                    o.ConnectionString = "localhost:6379";
                    o.EnableDistributedEviction = false;
                })))
            .Build();

        await host.StartAsync();
    }
}
