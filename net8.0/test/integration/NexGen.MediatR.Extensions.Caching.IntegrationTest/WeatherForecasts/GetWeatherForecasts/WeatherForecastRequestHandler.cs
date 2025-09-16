using MediatR;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Contracts;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.Dto;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.GetWeatherForecasts;

public class WeatherForecastRequestHandler : IRequestHandler<WeatherForecastRequest, IEnumerable<IResponse>>
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<IEnumerable<IResponse>> Handle(WeatherForecastRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(2000, cancellationToken);
        return Enumerable.Range(1, request.Limit).Select(index => new WeatherForecastDto
        {
            Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            TemperatureC = Random.Shared.Next(-20, 55),
            Summary = Summaries[Random.Shared.Next(Summaries.Length)]
        }).ToArray();
    }
}