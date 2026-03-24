using common.Messaging;
using SupplierWAL.Application.Common.Interfaces;

namespace SupplierWAL.Infrastructure.Messaging;

public sealed class QueueBackedQuotePublisher(IQueueService queueService) : IQuotePublisher
{
    public Task PublishAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken)
    {
        return queueService.PublishSupplierQuoteAsync(quote, cancellationToken);
    }
}
