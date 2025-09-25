namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public ushort Age { get; set; }

    public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;
}