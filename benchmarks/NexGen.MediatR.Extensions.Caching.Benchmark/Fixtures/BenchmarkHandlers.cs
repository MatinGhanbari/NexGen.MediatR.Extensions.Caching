using MediatR;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;

internal sealed class BenchmarkHandlers :
    IRequestHandler<CachedQuery, string>,
    IRequestHandler<OrderQuery, string>,
    IRequestHandler<ProductQuery, string>,
    IRequestHandler<UncachedQuery, string>
{
    private const string Result = "result";

    public Task<string> Handle(CachedQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result);

    public Task<string> Handle(OrderQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result);

    public Task<string> Handle(ProductQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result);

    public Task<string> Handle(UncachedQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(Result);
}
