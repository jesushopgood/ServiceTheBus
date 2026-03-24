namespace OrderService.Infrastructure.Persistence.Entities;

public sealed class OrderItemEntity
{
    public long Id { get; set; }

    public Guid OrderId { get; set; }

    public string Sku { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public decimal BasePrice { get; set; }

    public int Quantity { get; set; }

    public decimal LineTotal { get; set; }

    public OrderEntity? Order { get; set; }
}
