using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Constants;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.EntityFramework;

/// <summary>
/// EF Core interceptor that invalidates MediatR output-cache entries for changed entity types
/// after a successful save, either by publishing on an eviction bus or by calling the local invalidator.
/// </summary>
public class ChangeTrackerInterceptor : SaveChangesInterceptor
{
    private static readonly ConcurrentDictionary<DbContext, string[]> PendingTags = new();

    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChangeTrackerInterceptor"/> class.
    /// </summary>
    /// <param name="serviceProvider">Root service provider used to resolve publisher or invalidator.</param>
    public ChangeTrackerInterceptor(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        CapturePendingTags(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    /// <inheritdoc />
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        CapturePendingTags(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    /// <inheritdoc />
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        EvictOrPublish(eventData.Context, CancellationToken.None).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    /// <inheritdoc />
    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await EvictOrPublish(eventData.Context, cancellationToken).ConfigureAwait(false);
        return await base.SavedChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context is DbContext context)
            PendingTags.TryRemove(context, out _);

        base.SaveChangesFailed(eventData);
    }

    /// <inheritdoc />
    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is DbContext context)
            PendingTags.TryRemove(context, out _);

        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static void CapturePendingTags(DbContext? context)
    {
        if (context is null)
            return;

        var tags = context.ChangeTracker.Entries()
            .Where(static e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .Select(GetEntityTypeName)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (tags.Length == 0)
        {
            PendingTags.TryRemove(context, out _);
            return;
        }

        PendingTags[context] = tags;
    }

    private static string GetEntityTypeName(EntityEntry entry)
    {
        // Metadata.ClrType is the entity CLR type (not the EF proxy instance type).
        var clrType = entry.Metadata.ClrType;
        return clrType.Name;
    }

    private async Task EvictOrPublish(DbContext? context, CancellationToken cancellationToken)
    {
        if (context is null || !PendingTags.TryRemove(context, out var tags) || tags.Length == 0)
            return;

        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        var publisher = services.GetService<IRequestOutputCacheEvictionPublisher>();
        if (publisher is not null)
        {
            await publisher.PublishAsync(
                new RequestOutputCacheEvictionMessage { Tags = tags },
                cancellationToken).ConfigureAwait(false);
            return;
        }

        var cacheInvalidator = services.GetRequiredService<IRequestOutputCacheInvalidator>();
        var evictByTagsResult = await cacheInvalidator.EvictByTagsAsync(tags, cancellationToken).ConfigureAwait(false);
        if (evictByTagsResult.IsFailed)
            throw new InvalidOperationException(ErrorMessages.UnableToEvictEntitiesOnDbSaveChange);
    }
}
