using MediatR;
using NexGen.MediatR.Extensions.Caching.Contracts;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts;

public class WeatherForecastEvictRequestHandler : IRequestHandler<WeatherForecastEvictRequest, string>
{
    private readonly IRequestOutputCache<WeatherForecastEvictRequest, string> _cache;

    public WeatherForecastEvictRequestHandler(IRequestOutputCache<WeatherForecastEvictRequest, string> cache)
    {
        _cache = cache;
    }

    public async Task<string> Handle(WeatherForecastEvictRequest request, CancellationToken cancellationToken)
    {
        var tags = new List<string> { nameof(WeatherForecastDto) };
        await _cache.EvictByTagsAsync(tags, cancellationToken);

        return "Done!";
    }
}