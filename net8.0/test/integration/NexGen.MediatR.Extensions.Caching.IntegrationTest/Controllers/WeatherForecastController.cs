using IntegrationTest.WeatherForecasts;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationTest.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{


    private readonly ILogger<WeatherForecastController> _logger;
    private readonly IMediator _mediator;

    public WeatherForecastController(ILogger<WeatherForecastController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("/WeatherForecasts")]
    public async Task<IEnumerable<WeatherForecastDto>> GetWeatherForecastsAsync(int limit, int offset)
    {
        return await _mediator.Send(new WeatherForecastRequest { Limit = limit, Offset = offset });
    }

    [HttpGet("/WeatherForecasts/Evict")]
    public async Task<string> EvictWeatherForecastsAsync()
    {
        return await _mediator.Send(new WeatherForecastEvictRequest());
    }
}