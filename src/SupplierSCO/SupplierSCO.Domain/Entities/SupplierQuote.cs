namespace SupplierSCO.Domain.Entities;

public sealed class SupplierQuote
{
    public Guid OrderId { get; init; }

    public string SupplierCode { get; init; } = string.Empty;

    public IReadOnlyList<SupplierQuoteItem> Items { get; init; } = [];

    public decimal TotalPrice => Items.Sum(x => x.LineTotal);
}
