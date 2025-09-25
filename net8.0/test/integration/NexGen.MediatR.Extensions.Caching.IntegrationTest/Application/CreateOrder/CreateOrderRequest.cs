using MediatR;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.CreateOrder;

public sealed record CreateOrderRequest(Guid UserId, decimal TotalAmount) : IRequest;