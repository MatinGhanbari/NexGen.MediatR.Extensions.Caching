using MediatR;
using Microsoft.EntityFrameworkCore;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.GetUsers;

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
            .Include(x => x.Orders)
            .OrderByDescending(x => x.CreationDateTime)
            .Skip(request.Offset)
            .Take(request.Limit)
            .ToListAsync(cancellationToken: cancellationToken);
    }
}