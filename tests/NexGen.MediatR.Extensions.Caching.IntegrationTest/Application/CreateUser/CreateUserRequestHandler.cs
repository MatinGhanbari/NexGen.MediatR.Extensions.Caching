using MediatR;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.CreateUser;

public class CreateUserRequestHandler : IRequestHandler<CreateUserRequest>
{
    private readonly AppDbContext _dbContext;

    public CreateUserRequestHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        var user = new UserEntity(request.Name, request.Age);

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}