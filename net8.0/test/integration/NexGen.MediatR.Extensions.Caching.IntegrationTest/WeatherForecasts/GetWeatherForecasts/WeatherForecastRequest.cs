using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Contracts;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.Dto;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.GetWeatherForecasts;

[RequestOutputCache(tags: [nameof(WeatherForecastDto)], expirationInSeconds: 60)]
public class WeatherForecastRequest : IRequest<IEnumerable<IResponse>>
{
    public int Limit { get; set; } = 10;
    public int Offset { get; set; } = 0;
}