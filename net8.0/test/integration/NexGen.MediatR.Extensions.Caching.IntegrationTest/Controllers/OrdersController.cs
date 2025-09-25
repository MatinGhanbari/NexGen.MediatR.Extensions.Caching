using MediatR;
using Microsoft.AspNetCore.Mvc;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.CreateOrder;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.DeleteOrder;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.GetUserOrders;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ILogger<OrdersController> _logger;
    private readonly IMediator _mediator;

    public OrdersController(ILogger<OrdersController> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    [HttpGet("/api/[controller]/users/{userId:guid}/orders")]
    public async Task<List<OrderEntity>> GetOrdersAsync([FromRoute] Guid userId)
    {
        return await _mediator.Send(new GetUserOrdersRequest(userId));
    }

    [HttpPost("/api/[controller]/users/{userId:guid}/orders")]
    public async Task CreateOrderAsync([FromBody] CreateOrderRequest request)
    {
        await _mediator.Send(request);
    }

    [HttpDelete("/api/[controller]/users/{userId:guid}/orders/{orderId:guid}")]
    public async Task CreateOrderAsync([FromBody] DeleteOrderRequest request)
    {
        await _mediator.Send(request);
    }
}