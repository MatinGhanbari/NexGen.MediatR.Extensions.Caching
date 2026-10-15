using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.Hosting;

/// <summary>
/// Flushes all MediatR output cache entries once when the application host starts.
/// </summary>
internal sealed class RequestOutputCacheClearOnStartupHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RequestOutputCacheClearOnStartupHostedService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheClearOnStartupHostedService"/> class.
    /// </summary>
    public RequestOutputCacheClearOnStartupHostedService(
        IServiceScopeFactory scopeFactory,
        IServiceProvider serviceProvider)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = serviceProvider.GetService<ILogger<RequestOutputCacheClearOnStartupHostedService>>()
            ?? NullLogger<RequestOutputCacheClearOnStartupHostedService>.Instance;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var cacheInvalidator = scope.ServiceProvider.GetRequiredService<IRequestOutputCacheInvalidator>();
        var result = await cacheInvalidator.FlushAll(cancellationToken).ConfigureAwait(false);

        if (result.IsFailed)
            _logger.LogWarning("Failed to clear MediatR output cache on startup.");
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
