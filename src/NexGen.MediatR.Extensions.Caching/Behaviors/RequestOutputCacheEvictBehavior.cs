using FluentResults;
using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.Eviction;

namespace NexGen.MediatR.Extensions.Caching.Behaviors;

/// <summary>
/// Pipeline behavior that invalidates cache tags after a successful request marked with
/// <see cref="RequestOutputCacheEvictAttribute"/>.
/// </summary>
/// <typeparam name="TRequest">Request type.</typeparam>
/// <typeparam name="TResponse">Response type.</typeparam>
public sealed class RequestOutputCacheEvictBehavior<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly RequestOutputCacheEvictionDispatcher _dispatcher;

    /// <summary>
    /// Initializes a new instance of the <see cref="RequestOutputCacheEvictBehavior{TRequest, TResponse}"/> class.
    /// </summary>
    /// <param name="dispatcher">Dispatches local eviction and optional distributed notification.</param>
    public RequestOutputCacheEvictBehavior(RequestOutputCacheEvictionDispatcher dispatcher)
    {
        _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
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

        if (response is IResultBase { IsFailed: true })
            return response;

        await _dispatcher.DispatchAsync(attribute.Tags, cancellationToken).ConfigureAwait(false);
        return response;
    }
}
