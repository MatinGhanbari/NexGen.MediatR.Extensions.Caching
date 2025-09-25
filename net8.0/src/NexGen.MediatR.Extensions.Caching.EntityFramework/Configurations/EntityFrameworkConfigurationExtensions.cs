using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Configurations;

namespace NexGen.MediatR.Extensions.Caching.EntityFramework.Configurations;

public static class EntityFrameworkCoreConfigurationExtensions
{
    public static void UseEntityFrameworkAutoEvict<T>(this RequestOutputCacheConfigurationOption options, Action<DbContextOptionsBuilder> dbContextOptionsAction) where T : DbContext
    {
        if (options == null)
            throw new ArgumentNullException(nameof(options));

        if (dbContextOptionsAction == null)
            throw new ArgumentNullException(nameof(dbContextOptionsAction));

        options.Services.AddScoped<ChangeTrackerInterceptor>();

        options.Services.AddDbContext<T>((sp, dbOptions) =>
        {
            dbContextOptionsAction(dbOptions);
            dbOptions.AddInterceptors(sp.GetRequiredService<ChangeTrackerInterceptor>());
        });
    }
}