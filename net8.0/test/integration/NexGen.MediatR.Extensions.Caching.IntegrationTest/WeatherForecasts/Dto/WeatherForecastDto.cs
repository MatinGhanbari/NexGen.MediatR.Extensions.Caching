namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.Dto;

public class WeatherForecastDto
{
    public DateOnly Date { get; set; }
    public int TemperatureC { get; set; }
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
    public string? Summary { get; set; }
}