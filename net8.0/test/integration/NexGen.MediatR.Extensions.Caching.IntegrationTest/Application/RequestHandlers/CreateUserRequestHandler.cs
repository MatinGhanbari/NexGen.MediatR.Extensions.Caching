using MediatR;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.Requests;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.RequestHandlers;

public class CreateUserRequestHandler : IRequestHandler<CreateUserRequest>
{
    private readonly AppDbContext _dbContext;

    public CreateUserRequestHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CreateUserRequest request, CancellationToken cancellationToken)
    {
        await _dbContext.Users.AddAsync(new UserEntity()
        {
            Name = request.Name,
            Age = request.Age
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}