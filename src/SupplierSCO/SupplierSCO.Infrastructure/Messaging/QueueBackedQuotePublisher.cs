using common.Messaging;
using SupplierSCO.Application.Common.Interfaces;

namespace SupplierSCO.Infrastructure.Messaging;

public sealed class QueueBackedQuotePublisher(IQueueService queueService) : IQuotePublisher
{
    public Task PublishAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken)
    {
        return queueService.PublishSupplierQuoteAsync(quote, cancellationToken);
    }
}
