using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Infrastructure.Persistence.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class OrderItemEntityConfiguration : IEntityTypeConfiguration<OrderItemEntity>
{
    public void Configure(EntityTypeBuilder<OrderItemEntity> builder)
    {
        builder.ToTable("order_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Sku).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(256).IsRequired();
        builder.Property(x => x.BasePrice).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.LineTotal).HasPrecision(12, 2).IsRequired();

        builder.HasOne(x => x.Order)
            .WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
