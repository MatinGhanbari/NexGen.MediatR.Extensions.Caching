using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NexGen.MediatR.Extensions.Caching.Configurations;
using NexGen.MediatR.Extensions.Caching.Garnet.Configurations;
using NexGen.MediatR.Extensions.Caching.Redis.Configurations;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;

internal static class BenchmarkServiceProviderFactory
{
    public const string Memory = "Memory";
    public const string Redis = "Redis";
    public const string Garnet = "Garnet";

    public static IServiceProvider Create(string provider)
    {
        var services = new ServiceCollection();
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.SetMinimumLevel(LogLevel.None);
        });
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<BenchmarkHandlers>());
        services.AddMediatROutputCache(opt => ConfigureProvider(opt, provider));
        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    private static void ConfigureProvider(RequestOutputCacheConfigurationOption opt, string provider)
    {
        switch (provider)
        {
            case Redis:
                opt.UseRedisCache(redis =>
                {
                    redis.ConnectionString = DockerConnectivity.RedisConnectionString;
                    redis.InstanceName = "benchmarks";
                    redis.EnableDistributedEviction = false;
                });
                break;
            case Garnet:
                opt.UseGarnetCache(garnet =>
                {
                    garnet.ConnectionString = DockerConnectivity.GarnetConnectionString;
                    garnet.InstanceName = "benchmarks";
                    garnet.EnableDistributedEviction = false;
                });
                break;
            default:
                opt.UseMemoryCache();
                break;
        }
    }
}
