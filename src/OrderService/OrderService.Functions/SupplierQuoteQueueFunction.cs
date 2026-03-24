using System.Text.Json;
using Azure.Messaging.ServiceBus;
using common.Configuration;
using common.Messaging;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Application.Features.Orders.ProcessSupplierQuote;

namespace OrderService.Functions;

public sealed class SupplierQuoteQueueFunction(
    ISender sender,
    IOptions<MessageProcessingOptions> modeOptions,
    ILogger<SupplierQuoteQueueFunction> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(SupplierQuoteQueueFunction))]
    public async Task Run(
        [ServiceBusTrigger("%AzureServiceBusSupplierQuotesQueueName%", Connection = "AzureServiceBusConnectionString")]
        ServiceBusReceivedMessage message,
        CancellationToken cancellationToken)
    {
        MessageProcessingModeGuard.Ensure(modeOptions.Value, MessageProcessingMode.Function);

        var payload = message.Body.ToString();
        var quote = JsonSerializer.Deserialize<SupplierQuoteMessage>(payload, SerializerOptions);
        if (quote is null)
        {
            logger.LogWarning("SupplierQuoteQueueFunction::Run - Received invalid supplier quote payload.");
            return;
        }

        logger.LogInformation(
            "SupplierQuoteQueueFunction::Run - Received Order Total Value {TotalValue} for OrderId {OrderId}",
            quote.TotalPrice,
            quote.OrderId);

        await sender.Send(new ProcessSupplierQuoteCommand(quote), cancellationToken);

        logger.LogInformation("SupplierQuoteQueueFunction::Run - SUCCESSFULLY Processed Message");
    }
}
