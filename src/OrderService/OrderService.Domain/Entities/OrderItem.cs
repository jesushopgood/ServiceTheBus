namespace OrderService.Domain.Entities;

public sealed class OrderItem
{
    public string Sku { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal BasePrice { get; init; }

    public int Quantity { get; init; }

    public decimal LineTotal => BasePrice * Quantity;
}
