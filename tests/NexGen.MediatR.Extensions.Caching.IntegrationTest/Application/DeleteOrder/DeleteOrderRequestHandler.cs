using MediatR;
using Microsoft.EntityFrameworkCore;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.DeleteOrder;

public class DeleteOrderRequestHandler : IRequestHandler<DeleteOrderRequest>
{
    private readonly AppDbContext _dbContext;

    public DeleteOrderRequestHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(DeleteOrderRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.Include(x => x.Orders).FirstOrDefaultAsync(x => x.Id.Equals(request.UserId), cancellationToken);
        if (user == null) return;

        var order = user.Orders.FirstOrDefault(x => x.Id.Equals(request.OrderId));
        if (order == null) return;

        user.Orders.Remove(order);

        _dbContext.Orders.Remove(order);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}