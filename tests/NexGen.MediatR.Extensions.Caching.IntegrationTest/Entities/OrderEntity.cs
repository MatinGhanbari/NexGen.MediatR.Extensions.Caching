namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

public sealed class OrderEntity
{
    public OrderEntity(decimal totalAmount)
    {
        TotalAmount = totalAmount;
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public decimal TotalAmount { get; set; }
    public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;
}