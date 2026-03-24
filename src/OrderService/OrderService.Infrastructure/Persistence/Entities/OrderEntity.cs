namespace OrderService.Infrastructure.Persistence.Entities;

public sealed class OrderEntity
{
    public Guid OrderId { get; set; }

    public DateTime OrderDate { get; set; }

    public decimal TotalPrice { get; set; }

    public List<OrderItemEntity> Items { get; set; } = [];
}
