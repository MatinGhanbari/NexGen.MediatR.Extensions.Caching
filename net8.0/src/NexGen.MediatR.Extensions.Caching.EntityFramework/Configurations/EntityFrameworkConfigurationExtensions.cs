using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NexGen.MediatR.Extensions.Caching.Configurations;

namespace NexGen.MediatR.Extensions.Caching.EntityFramework.Configurations;

public static class EntityFrameworkCoreConfigurationExtensions
{
    public static void UseMediatROutputCacheAutoEvict(this DbContextOptionsBuilder optionsBuilder, IServiceProvider serviceProvider)
    {
        if (optionsBuilder == null)
            throw new ArgumentNullException(nameof(optionsBuilder));

        var interceptor = new ChangeTrackerInterceptor(serviceProvider);
        optionsBuilder.AddInterceptors(interceptor);
    }
}