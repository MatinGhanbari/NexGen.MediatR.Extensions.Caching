using FluentResults;
using MediatR;

namespace NexGen.MediatR.Extensions.Caching.Contracts;

public interface IRequestOutputCache<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    Task<Result<TResponse>> GetAsync(TRequest request, CancellationToken cancellationToken = default);
    Task<Result> SetAsync(TRequest request, TResponse response, IEnumerable<string>? tags = null, int expirationInSeconds = default, CancellationToken cancellationToken = default);
    Task<Result> EvictByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default);
}