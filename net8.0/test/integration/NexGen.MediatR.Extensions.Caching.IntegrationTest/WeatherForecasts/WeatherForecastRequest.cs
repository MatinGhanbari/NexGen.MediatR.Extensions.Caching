using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts;

[RequestOutputCache([nameof(WeatherForecastDto)], expirationInSeconds: 10)]
public class WeatherForecastRequest : IRequest<IEnumerable<WeatherForecastDto>>
{
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
}