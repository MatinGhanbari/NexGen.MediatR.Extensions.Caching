using FluentResults;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Hosting;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Configurations;

public sealed class ClearCacheOnStartupTests
{
    [Fact]
    public void ClearCacheOnStartup_RegistersHostedService_WithoutBuildingServiceProviderDuringRegistration()
    {
        var services = new ServiceCollection();
        services.AddMediatROutputCache(opt =>
        {
            opt.UseMemoryCache();
            opt.ClearCacheOnStartup();
        });

        Assert.Contains(services, d =>
            d.ServiceType == typeof(IHostedService)
            && d.ImplementationType == typeof(RequestOutputCacheClearOnStartupHostedService));
    }

    [Fact]
    public void ClearCacheOnStartup_WithoutProvider_ThrowsDuringRegistration()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddMediatROutputCache(opt => opt.ClearCacheOnStartup()));

        Assert.Equal(ErrorMessages.CacheProviderNotConfigured, exception.Message);
    }

    [Fact]
    public void ClearCacheOnStartup_CalledTwice_RegistersHostedServiceOnce()
    {
        var services = new ServiceCollection();
        services.AddMediatROutputCache(opt =>
        {
            opt.UseMemoryCache();
            opt.ClearCacheOnStartup();
            opt.ClearCacheOnStartup();
        });

        Assert.Equal(1, services.Count(d =>
            d.ImplementationType == typeof(RequestOutputCacheClearOnStartupHostedService)));
    }

    [Fact]
    public async Task ClearCacheOnStartupHostedService_FlushesCacheWhenHostStarts()
    {
        var invalidator = new CapturingInvalidator();

        using var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddScoped<IRequestOutputCacheInvalidator>(_ => invalidator);
                services.AddHostedService<RequestOutputCacheClearOnStartupHostedService>();
            })
            .Build();

        await host.StartAsync();

        Assert.True(invalidator.FlushAllCalled);
    }

    private sealed class CapturingInvalidator : IRequestOutputCacheInvalidator
    {
        public bool FlushAllCalled { get; private set; }

        public Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
            => Task.FromResult(Result.Ok());

        public Task<Result> FlushAll(CancellationToken cancellationToken = default)
        {
            FlushAllCalled = true;
            return Task.FromResult(Result.Ok());
        }
    }
}
