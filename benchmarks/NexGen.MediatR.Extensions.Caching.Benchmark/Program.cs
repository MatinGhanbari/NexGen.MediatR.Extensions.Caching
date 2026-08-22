using BenchmarkDotNet.Running;
using NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Eviction;
using NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Micro;
using NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Pipeline;
using NexGen.MediatR.Extensions.Caching.Benchmark.Benchmarks.Providers;
using NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;

var suite = args.FirstOrDefault()?.Trim().ToLowerInvariant() ?? "all";
var emptyArgs = Array.Empty<string>();

switch (suite)
{
    case "pipeline":
        BenchmarkRunner.Run<CachePipelineBenchmark>(args: emptyArgs);
        break;
    case "micro":
        BenchmarkRunner.Run<CacheKeyBenchmark>(args: emptyArgs);
        BenchmarkRunner.Run<ContainerBenchmark>(args: emptyArgs);
        break;
    case "eviction":
        BenchmarkRunner.Run<TagEvictionBenchmark>(args: emptyArgs);
        break;
    case "provider":
        RunProviderSuites(emptyArgs);
        break;
    case "all":
        BenchmarkRunner.Run<CachePipelineBenchmark>(args: emptyArgs);
        BenchmarkRunner.Run<CacheKeyBenchmark>(args: emptyArgs);
        BenchmarkRunner.Run<ContainerBenchmark>(args: emptyArgs);
        BenchmarkRunner.Run<TagEvictionBenchmark>(args: emptyArgs);
        RunProviderSuites(emptyArgs);
        break;
    default:
        Console.Error.WriteLine("Usage: [all|pipeline|micro|eviction|provider]");
        return 1;
}

return 0;

static void RunProviderSuites(string[] emptyArgs)
{
    BenchmarkRunner.Run<MemoryProviderGetSetBenchmark>(args: emptyArgs);

    if (DockerConnectivity.IsRedisAvailable())
    {
        BenchmarkRunner.Run<RedisProviderGetSetBenchmark>(args: emptyArgs);
    }
    else
    {
        Console.WriteLine("Skipping Redis provider benchmarks: Redis is not reachable on localhost:6379.");
    }

    if (DockerConnectivity.IsGarnetAvailable())
    {
        BenchmarkRunner.Run<GarnetProviderGetSetBenchmark>(args: emptyArgs);
    }
    else
    {
        Console.WriteLine("Skipping Garnet provider benchmarks: Garnet is not reachable on localhost:6380.");
    }
}
