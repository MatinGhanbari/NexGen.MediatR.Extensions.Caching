using MediatR;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts;

public class WeatherForecastRequestHandler : IRequestHandler<WeatherForecastRequest, IEnumerable<WeatherForecastDto>>
{
    private static readonly string[] Summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    public async Task<IEnumerable<WeatherForecastDto>> Handle(WeatherForecastRequest request, CancellationToken cancellationToken)
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