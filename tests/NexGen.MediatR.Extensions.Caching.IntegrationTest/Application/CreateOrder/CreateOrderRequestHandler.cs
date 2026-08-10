using MediatR;
using Microsoft.EntityFrameworkCore;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Application.CreateOrder;

public class CreateOrderRequestHandler : IRequestHandler<CreateOrderRequest>
{
    private readonly AppDbContext _dbContext;

    public CreateOrderRequestHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.Include(x => x.Orders).FirstOrDefaultAsync(x => x.Id.Equals(request.UserId), cancellationToken);
        if (user == null) return;

        var order = new OrderEntity(request.TotalAmount);
        user.Orders.Add(order);

        await _dbContext.Orders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}