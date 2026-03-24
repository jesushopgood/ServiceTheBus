namespace OrderService.Domain.Entities;

public sealed class Order
{
    public Guid OrderId { get; init; }

    public DateTime OrderDate { get; init; }

    public IReadOnlyList<OrderItem> Items { get; init; } = [];

    public decimal TotalPrice => Items.Sum(x => x.LineTotal);
}
