using MediatR;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Performance
{
    internal class SimpleRequestsHandlers :
        IRequestHandler<SimpleNotCachedRequest, string>,
        IRequestHandler<SimpleCachedRequest, string>
    {
        private readonly string _result = "JOB RESULT";

        public async Task<string> Handle(SimpleNotCachedRequest request, CancellationToken cancellationToken)
        {
            return await DoTheJob(cancellationToken);
        }

        public async Task<string> Handle(SimpleCachedRequest request, CancellationToken cancellationToken)
        {
            return await DoTheJob(cancellationToken);
        }

        private async Task<string> DoTheJob(CancellationToken cancellationToken = default)
        {
            // Simulate the job
            await Task.Delay(new Random().Next(200, 600), cancellationToken);
            return _result;
        }
    }
}
