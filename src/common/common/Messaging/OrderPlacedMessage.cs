namespace common.Messaging;

public sealed record OrderPlacedMessage(
    Guid OrderId,
    DateTime OrderDate,
    IReadOnlyList<OrderPlacedItemMessage> Items);

public sealed record OrderPlacedItemMessage(
    string Sku,
    string Description,
    int Quantity);
