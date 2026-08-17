using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.CreateUser;

[RequestOutputCacheEvict(nameof(UserEntity), nameof(OrderEntity))]
public sealed record CreateUserRequest(string Name, ushort Age) : IRequest;
