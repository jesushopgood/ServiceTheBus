namespace OrderService.Infrastructure.Persistence.Entities;

public sealed class QuoteEntity
{
    public long Id { get; set; }

    public Guid OrderId { get; set; }

    public string ServiceName { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }

    public bool IsCheapest { get; set; }

    public DateTime ReceivedAtUtc { get; set; }
}
