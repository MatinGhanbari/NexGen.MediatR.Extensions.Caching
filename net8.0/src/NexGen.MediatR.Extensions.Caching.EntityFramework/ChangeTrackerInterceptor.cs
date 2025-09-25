using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.EntityFramework;

public class ChangeTrackerInterceptor : SaveChangesInterceptor
{
    private readonly IServiceProvider _serviceProvider;

    public ChangeTrackerInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not DbContext context)
            return await base.SavedChangesAsync(eventData, result, cancellationToken);

        var entries = context.ChangeTracker.Entries();
        var test = entries.ToList();

        using var scope = _serviceProvider.CreateScope();
        var cacheInvalidator = scope.ServiceProvider.GetRequiredService<IRequestOutputCacheInvalidator>();

        var types = entries.Select(e => e.Entity.GetType().Name).Distinct().ToArray();

        var evictByTagsResult = await cacheInvalidator.EvictByTagsAsync(types, cancellationToken);
        if (evictByTagsResult.IsFailed)
            throw new Exception(ErrorMessages.UnableToEvictEntitiesOnDbSaveChange);

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}