namespace common.Messaging;

public sealed record SupplierQuoteMessage(
    Guid OrderId,
    string SupplierCode,
    decimal TotalPrice,
    IReadOnlyList<SupplierQuoteItemMessage> Items);

public sealed record SupplierQuoteItemMessage(
    string Sku,
    string Description,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal);
