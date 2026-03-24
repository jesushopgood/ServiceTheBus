using common.Messaging;

namespace SupplierSCO.Application.Common.Interfaces;

public interface IQuotePublisher
{
    Task PublishAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken);
}
