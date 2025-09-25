using MediatR;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.Requests;

public sealed record CreateUserRequest(string Name, ushort Age) : IRequest;