namespace OrderService.Domain.Entities;

public sealed class Product
{
    public int Id { get; init; }

    public string Sku { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal BasePrice { get; init; }
}
