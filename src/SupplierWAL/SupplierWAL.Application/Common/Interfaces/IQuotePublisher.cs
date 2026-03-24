using common.Messaging;

namespace SupplierWAL.Application.Common.Interfaces;

public interface IQuotePublisher
{
    Task PublishAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken);
}
