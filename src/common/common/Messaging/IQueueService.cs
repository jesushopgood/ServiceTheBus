namespace common.Messaging;

public interface IQueueService
{
    Task PublishOrderAsync(OrderPlacedMessage order, CancellationToken cancellationToken);

    Task PublishSupplierQuoteAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken);

    Task<bool> ProcessNextOrderAsync(
        string supplierCode,
        Func<OrderPlacedMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken);

    Task<bool> ProcessNextSupplierQuoteAsync(
        Func<SupplierQuoteMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken);
}
