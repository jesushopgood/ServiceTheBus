using common.Messaging;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.Repositories;

[Obsolete("InMemoryQuoteAggregationRepository has been replaced by EF Core persistence. Use EfQuoteAggregationRepository.")]
public sealed class InMemoryQuoteAggregationRepository(EfQuoteAggregationRepository innerRepository) : IQuoteAggregationRepository
{
    public Task<int> AddQuoteAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken)
    {
        return innerRepository.AddQuoteAsync(quote, cancellationToken);
    }

    public Task<IReadOnlyList<SupplierQuoteMessage>> GetQuotesAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return innerRepository.GetQuotesAsync(orderId, cancellationToken);
    }

    public Task MarkCheapestAsync(Guid orderId, string supplierCode, CancellationToken cancellationToken)
    {
        return innerRepository.MarkCheapestAsync(orderId, supplierCode, cancellationToken);
    }
}
