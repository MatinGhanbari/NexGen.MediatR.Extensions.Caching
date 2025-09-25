using MediatR;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Requests;

public sealed record CreateUserRequest(string Name, ushort Age) : IRequest;