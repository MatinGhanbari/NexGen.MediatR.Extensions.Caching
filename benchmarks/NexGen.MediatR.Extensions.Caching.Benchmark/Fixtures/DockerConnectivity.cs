using System.Net.Sockets;

namespace NexGen.MediatR.Extensions.Caching.Benchmark.Fixtures;

internal static class DockerConnectivity
{
    public const string RedisConnectionString = "localhost:6379,password=P@ssword,abortConnect=false";
    public const string GarnetConnectionString = "localhost:6380,abortConnect=false";

    public static bool IsRedisAvailable() => IsPortOpen("127.0.0.1", 6379);

    public static bool IsGarnetAvailable() => IsPortOpen("127.0.0.1", 6380);

    private static bool IsPortOpen(string host, int port)
    {
        try
        {
            using var client = new TcpClient();
            var result = client.BeginConnect(host, port, null, null);
            var success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(400), false);
            if (!success)
                return false;

            client.EndConnect(result);
            return client.Connected;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
