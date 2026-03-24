using System.Text.Json;
using Azure.Messaging.ServiceBus;
using common.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierENG.Application.Common.Interfaces;

namespace SupplierENG.Infrastructure.Messaging;

public sealed class ServiceBusQuotePublisher(
    IOptions<SupplierServiceBusOptions> options,
    ILogger<ServiceBusQuotePublisher> logger) : IQuotePublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SupplierServiceBusOptions _options = options.Value;

    public async Task PublishAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString) || string.IsNullOrWhiteSpace(_options.SupplierQuotesQueueName))
        {
            throw new InvalidOperationException("SupplierServiceBus queue publishing settings are missing.");
        }

        await using var client = new ServiceBusClient(_options.ConnectionString);
        ServiceBusSender sender = client.CreateSender(_options.SupplierQuotesQueueName);

        var body = JsonSerializer.Serialize(quote, SerializerOptions);
        var message = new ServiceBusMessage(body)
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = quote.OrderId.ToString(),
            Subject = "supplier.quote",
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken);
        logger.LogInformation("Published quote for order {OrderId} from supplier {SupplierCode}", quote.OrderId, quote.SupplierCode);
    }
}
