using MediatR;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Performance
{
    internal class SimpleRequestsHandlers :
        IRequestHandler<SimpleNotCachedRequest, Task>,
        IRequestHandler<SimpleCachedRequest, Task>
    {
        public async Task<Task> Handle(SimpleNotCachedRequest request, CancellationToken cancellationToken)
        {
            return DoTheJob();
        }

        public async Task<Task> Handle(SimpleCachedRequest request, CancellationToken cancellationToken)
        {
            return DoTheJob();
        }

        private async Task DoTheJob()
        {
            // Simulate the job
            Thread.Sleep(new Random().Next(500, 1500));
        }
    }
}
