namespace SupplierSCO.Domain.Entities;

public sealed class SupplierQuoteItem
{
    public string Sku { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public int Quantity { get; init; }

    public decimal UnitPrice { get; init; }

    public decimal LineTotal => UnitPrice * Quantity;
}
