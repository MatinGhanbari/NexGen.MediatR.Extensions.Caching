using MediatR;
using Microsoft.AspNetCore.Mvc;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("/api/v1/[controller]")]
public class WeatherForecastController : ControllerBase
{
    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IMediator _mediator;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("WeatherForecasts")]
    public async Task<IEnumerable<WeatherForecastDto>> GetWeatherForecastsAsync(int limit = 10, int offset = 0)
    {
        return await _mediator.Send(new WeatherForecastRequest { Limit = limit, Offset = offset });
    }

    [HttpGet("WeatherForecasts/Evict")]
    public async Task<string> EvictWeatherForecastsAsync()
    {
        return await _mediator.Send(new WeatherForecastEvictRequest());
    }
}