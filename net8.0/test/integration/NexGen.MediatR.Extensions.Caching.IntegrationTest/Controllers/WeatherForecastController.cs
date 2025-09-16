using MediatR;
using Microsoft.AspNetCore.Mvc;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Contracts;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.Dto;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.EvictWeatherForecasts;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.WeatherForecasts.GetWeatherForecasts;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Controllers;

[ApiController]
[Route("/api")]
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
    public async Task<IEnumerable<IResponse>> GetWeatherForecastsAsync(int limit = 10, int offset = 0)
    {
        return await _mediator.Send(new WeatherForecastRequest { Limit = limit, Offset = offset });
    }

    [HttpPost("WeatherForecasts/Evict")]
    public async Task<string> EvictWeatherForecastsAsync()
    {
        return await _mediator.Send(new WeatherForecastEvictRequest());
    }
}