using System.Collections.Concurrent;
using MediatR;

namespace NexGen.MediatR.Extensions.Caching.Configurations;

/// <summary>
/// Registry of per-request predicates that decide whether a handler response should be cached.
/// Registered as a singleton and populated via
/// <see cref="RequestOutputCacheConfigurationOption.CacheWhen{TRequest, TResponse}(Func{TResponse, bool})"/>.
/// </summary>
public sealed class RequestOutputCacheConditions
{
    private readonly ConcurrentDictionary<(Type Request, Type Response), Delegate> _predicates = new();

    /// <summary>
    /// Registers a predicate for <typeparamref name="TRequest"/> / <typeparamref name="TResponse"/>.
    /// A later registration for the same pair replaces the previous one.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type.</typeparam>
    /// <typeparam name="TResponse">The MediatR response type.</typeparam>
    /// <param name="predicate">
    /// Returns <see langword="true"/> when the response should be cached.
    /// Exceptions thrown by the predicate are not caught.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="predicate"/> is <see langword="null"/>.</exception>
    public void Register<TRequest, TResponse>(Func<TRequest, TResponse, bool> predicate)
        where TRequest : IRequest<TResponse>
    {
        ArgumentNullException.ThrowIfNull(predicate);
        _predicates[(typeof(TRequest), typeof(TResponse))] = predicate;
    }

    /// <summary>
    /// Returns the predicate registered for <typeparamref name="TRequest"/> / <typeparamref name="TResponse"/>,
    /// or <see langword="null"/> when none is registered.
    /// </summary>
    /// <typeparam name="TRequest">The MediatR request type.</typeparam>
    /// <typeparam name="TResponse">The MediatR response type.</typeparam>
    /// <returns>The registered predicate, or <see langword="null"/>.</returns>
    public Func<TRequest, TResponse, bool>? Find<TRequest, TResponse>()
        where TRequest : IRequest<TResponse>
    {
        if (_predicates.TryGetValue((typeof(TRequest), typeof(TResponse)), out var predicate))
            return (Func<TRequest, TResponse, bool>)predicate;

        return null;
    }
}
