using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.GetUserOrders;

[RequestOutputCache(tags: [nameof(UserEntity), nameof(OrderEntity)], expirationInSeconds: 300)]
public sealed record GetUserOrdersRequest(Guid UserId) : IRequest<List<OrderEntity>>;