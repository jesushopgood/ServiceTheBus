using common.Messaging;

namespace SupplierENG.Application.Common.Interfaces;

public interface IQuotePublisher
{
    Task PublishAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken);
}
