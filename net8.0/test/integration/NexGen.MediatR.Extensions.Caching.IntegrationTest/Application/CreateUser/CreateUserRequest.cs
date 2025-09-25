using MediatR;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.CreateUser;

public sealed record CreateUserRequest(string Name, ushort Age) : IRequest;