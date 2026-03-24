using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Infrastructure.Persistence.Entities;
using OrderService.Infrastructure.Persistence.Seeding;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class ProductEntityConfiguration : IEntityTypeConfiguration<ProductEntity>
{
    public void Configure(EntityTypeBuilder<ProductEntity> builder)
    {
        builder.ToTable("products");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Sku).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(256).IsRequired();
        builder.Property(x => x.BasePrice).HasPrecision(12, 2).IsRequired();
        builder.HasIndex(x => x.Sku).IsUnique();

        builder.HasData(ProductSeedData.All);
    }
}
