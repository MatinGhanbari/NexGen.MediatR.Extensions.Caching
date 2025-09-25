using MediatR;
using NexGen.MediatR.Extensions.Caching.Attributes;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.Requests;

[RequestOutputCache(tags: [nameof(UserEntity)], expirationInSeconds: 300)]
public sealed record GetUsersRequest(int Limit = 10, int Offset = 0) : IRequest<List<UserEntity>>;