using MediatR;
using Microsoft.EntityFrameworkCore;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.GetUserOrders;


public class GetUserOrdersRequestHandler : IRequestHandler<GetUserOrdersRequest, List<OrderEntity>>
{
    private readonly AppDbContext _dbContext;

    public GetUserOrdersRequestHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<OrderEntity>> Handle(GetUserOrdersRequest request, CancellationToken cancellationToken)
    {
        await Task.Delay(500, cancellationToken);

        var user = await _dbContext.Users.Include(x => x.Orders).FirstOrDefaultAsync(x => x.Id.Equals(request.UserId), cancellationToken);
        return user?.Orders!;
    }
}