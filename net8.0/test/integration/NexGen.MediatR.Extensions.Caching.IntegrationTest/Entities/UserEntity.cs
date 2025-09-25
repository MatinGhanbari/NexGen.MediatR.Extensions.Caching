namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

public sealed class UserEntity
{
    public UserEntity(string name, ushort age)
    {
        Name = name;
        Age = age;
    }

    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; }
    public ushort Age { get; set; }
    public DateTime CreationDateTime { get; set; } = DateTime.UtcNow;
    public List<OrderEntity> Orders { get; set; } = [];
}