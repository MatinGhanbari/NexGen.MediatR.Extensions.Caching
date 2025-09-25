using MediatR;
using Microsoft.AspNetCore.Mvc;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.Requests;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly ILogger<UsersController> _logger;
    private readonly IMediator _mediator;

    public UsersController(ILogger<UsersController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<List<UserEntity>> GetUsersAsync([FromQuery] int limit = 10, [FromQuery] int offset = 0)
    {
        return await _mediator.Send(new GetUsersRequest(limit, offset));
    }

    [HttpPost]
    public async Task CreateUserAsync([FromBody] CreateUserRequest request)
    {
        await _mediator.Send(request);
    }
}