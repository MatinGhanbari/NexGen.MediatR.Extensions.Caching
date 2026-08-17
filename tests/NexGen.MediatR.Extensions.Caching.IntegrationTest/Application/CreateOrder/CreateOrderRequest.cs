using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.CreateOrder;

[RequestOutputCacheEvict(nameof(UserEntity), nameof(OrderEntity))]
public sealed record CreateOrderRequest(Guid UserId, decimal TotalAmount) : IRequest;
