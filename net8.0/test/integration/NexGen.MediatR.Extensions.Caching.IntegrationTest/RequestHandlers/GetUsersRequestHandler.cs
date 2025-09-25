using MediatR;
using Microsoft.EntityFrameworkCore;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Requests;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.RequestHandlers;

public class GetUsersRequestHandler : IRequestHandler<GetUsersRequest, List<UserEntity>>
{
    private readonly AppDbContext _dbContext;

    public GetUsersRequestHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<UserEntity>> Handle(GetUsersRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(500, cancellationToken);
        return await _dbContext.Users
            .AsNoTracking()
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(cancellationToken: cancellationToken);
    }
}