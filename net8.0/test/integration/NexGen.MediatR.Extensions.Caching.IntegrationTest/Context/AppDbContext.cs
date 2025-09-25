using Microsoft.EntityFrameworkCore;
using NexGen.MediatR.Extensions.Caching.IntegrationTest.Entities;

namespace NexGen.MediatR.Extensions.Caching.IntegrationTest.Context;

public class AppDbContext : DbContext
{
    public DbSet<UserEntity> Users { get; set; }
    public DbSet<OrderEntity> Orders { get; set; }

    public AppDbContext(DbContextOptions dbContextOptions) : base(dbContextOptions)
    {

    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.Name).IsRequired().HasMaxLength(100);
            entity.Property(u => u.Age).IsRequired();
            entity.Property(u => u.CreationDateTime).IsRequired();
            entity.HasMany(u => u.Orders);
        });

        modelBuilder.Entity<OrderEntity>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.Property(u => u.TotalAmount).IsRequired();
            entity.Property(u => u.CreationDateTime).IsRequired();
        });
    }
}