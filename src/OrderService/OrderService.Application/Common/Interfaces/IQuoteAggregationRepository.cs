using common.Messaging;

namespace OrderService.Application.Common.Interfaces;

public interface IQuoteAggregationRepository
{
    Task<int> AddQuoteAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken);

    Task<IReadOnlyList<SupplierQuoteMessage>> GetQuotesAsync(Guid orderId, CancellationToken cancellationToken);

    Task MarkCheapestAsync(Guid orderId, string supplierCode, CancellationToken cancellationToken);
}
