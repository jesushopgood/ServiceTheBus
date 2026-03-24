namespace OrderService.Infrastructure.Persistence.Entities;

public sealed class ProductEntity
{
    public int Id { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }
}
