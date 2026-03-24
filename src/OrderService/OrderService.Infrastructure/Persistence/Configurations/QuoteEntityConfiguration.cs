using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Infrastructure.Persistence.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class QuoteEntityConfiguration : IEntityTypeConfiguration<QuoteEntity>
{
    public void Configure(EntityTypeBuilder<QuoteEntity> builder)
    {
        builder.ToTable("quotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.ServiceName).HasMaxLength(8).IsRequired();
        builder.Property(x => x.TotalPrice).HasPrecision(12, 2).IsRequired();
        builder.Property(x => x.ReceivedAtUtc).IsRequired();

        builder.HasIndex(x => new { x.OrderId, x.ServiceName }).IsUnique();
        builder.HasIndex(x => new { x.OrderId, x.IsCheapest });
    }
}
