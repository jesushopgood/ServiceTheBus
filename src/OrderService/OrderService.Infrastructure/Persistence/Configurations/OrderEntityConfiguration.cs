using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Infrastructure.Persistence.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class OrderEntityConfiguration : IEntityTypeConfiguration<OrderEntity>
{
    public void Configure(EntityTypeBuilder<OrderEntity> builder)
    {
        builder.ToTable("orders");
        builder.HasKey(x => x.OrderId);
        builder.Property(x => x.OrderDate).IsRequired();
        builder.Property(x => x.TotalPrice).HasPrecision(12, 2).IsRequired();
    }
}
