using MediatR;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.DeleteOrder;

public sealed record DeleteOrderRequest(Guid UserId, Guid OrderId) : IRequest;