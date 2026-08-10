using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Contracts;
using NexGen.MediatR.Extensions.Caching.Messages;

namespace NexGen.MediatR.Extensions.Caching.Behaviors;

/// <summary>
/// Pipeline behavior that invalidates cache tags after a successful request marked with
/// <see cref="RequestOutputCacheEvictAttribute"/>.
/// Prefers <see cref="IRequestOutputCacheEvictionPublisher"/> when registered; otherwise uses
/// <see cref="IRequestOutputCacheInvalidator"/>.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class RequestOutputCacheEvictBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheEvictBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="serviceProvider">Service provider used to resolve optional publisher/invalidator.</param>
    public RequestOutputCacheEvictBehavior(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default)
    {
        var attribute = (RequestOutputCacheEvictAttribute?)typeof(TRequest)
            .GetCustomAttributes(typeof(RequestOutputCacheEvictAttribute), false)
            .FirstOrDefault();

        var response = await next(cancellationToken).ConfigureAwait(false);

        if (attribute?.Tags is null || attribute.Tags.Length == 0)
            return response;

        var publisher = _serviceProvider.GetService<IRequestOutputCacheEvictionPublisher>();
        if (publisher is not null)
        {
            await publisher.PublishAsync(
                new RequestOutputCacheEvictionMessage { Tags = attribute.Tags },
                cancellationToken).ConfigureAwait(false);
            return response;
        }

        var invalidator = _serviceProvider.GetService<IRequestOutputCacheInvalidator>();
        if (invalidator is not null)
        {
            await invalidator.EvictByTagsAsync(attribute.Tags, cancellationToken).ConfigureAwait(false);
        }

        return response;
    }
}
