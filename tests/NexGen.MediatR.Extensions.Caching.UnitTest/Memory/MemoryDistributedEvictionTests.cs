using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.UnitTest.Memory;

public sealed class MemoryDistributedEvictionTests
{
    [Fact]
    public void UseMemoryCache_DoesNotRegisterDistributedEviction()
    {
        var services = new ServiceCollection();
        services.AddMediatROutputCache(opt => opt.UseMemoryCache());

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IRequestOutputCacheEvictionNotifier));
        Assert.DoesNotContain(services, d =>
            d.ImplementationType is not null
            && d.ImplementationType.Name.Contains("EvictionListener", StringComparison.Ordinal));
    }
}
