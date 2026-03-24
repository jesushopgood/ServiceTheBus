using Microsoft.EntityFrameworkCore;
using OrderService.Infrastructure.Persistence.Configurations;
using OrderService.Infrastructure.Persistence.Entities;

namespace OrderService.Infrastructure.Persistence;

public sealed class OrderServiceDbContext(DbContextOptions<OrderServiceDbContext> options) : DbContext(options)
{
    public DbSet<ProductEntity> Products => Set<ProductEntity>();

    public DbSet<OrderEntity> Orders => Set<OrderEntity>();

    public DbSet<OrderItemEntity> OrderItems => Set<OrderItemEntity>();

    public DbSet<QuoteEntity> Quotes => Set<QuoteEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ProductEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrderEntityConfiguration());
        modelBuilder.ApplyConfiguration(new OrderItemEntityConfiguration());
        modelBuilder.ApplyConfiguration(new QuoteEntityConfiguration());
    }
}
