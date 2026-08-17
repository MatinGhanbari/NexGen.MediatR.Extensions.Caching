using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.DeleteOrder;

[RequestOutputCacheEvict(nameof(UserEntity), nameof(OrderEntity))]
public sealed record DeleteOrderRequest(Guid UserId, Guid OrderId) : IRequest;
