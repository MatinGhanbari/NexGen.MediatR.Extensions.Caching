using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;

namespace NexGen.MediatR.Extensions.Caching.Benchmark;

internal sealed class ShortRunConfig : ManualConfig
{
    public ShortRunConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(3)
            .WithIterationCount(8)
            .WithId("short"));
        AddDiagnoser(MemoryDiagnoser.Default);
        AddColumn(RankColumn.Arabic);
        WithOrderer(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
        WithOptions(ConfigOptions.DisableOptimizationsValidator);
    }
}
