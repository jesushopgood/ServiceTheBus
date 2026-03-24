using System.Text.Json;
using Azure.Messaging.ServiceBus;
using common.Configuration;
using common.Messaging;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SupplierENG.Application.Common.Interfaces;
using SupplierENG.Application.Features.Orders.BuildQuote;
using SupplierENG.Infrastructure.Messaging;

namespace SupplierENG.Functions;

public sealed class OrderTopicFunction(
    ISender sender,
    IQuotePublisher quotePublisher,
    IOptions<MessageProcessingOptions> modeOptions,
    IOptions<SupplierServiceBusOptions> supplierOptions,
    ILogger<OrderTopicFunction> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    [Function(nameof(OrderTopicFunction))]
    public async Task Run(
        [ServiceBusTrigger("%SupplierServiceBusOrdersTopicName%", "%SupplierServiceBusOrdersSubscriptionName%", Connection = "SupplierServiceBusConnectionString")]
        ServiceBusReceivedMessage receivedMessage,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("OrderTopicFunction::Run - Function invoked by trigger");

        try
        {
            MessageProcessingModeGuard.Ensure(modeOptions.Value, MessageProcessingMode.Function);
        }
        catch (InvalidOperationException guardEx)
        {
            logger.LogError(guardEx, "OrderTopicFunction::Run - MessageProcessingModeGuard failed. Mode: {Mode}", modeOptions.Value.Mode);
            throw;
        }

        var message = JsonSerializer.Deserialize<OrderPlacedMessage>(receivedMessage.Body.ToString(), SerializerOptions);
        if (message is null)
        {
            logger.LogWarning("OrderTopicFunction::Run - Received invalid order payload.");
            return;
        }

        logger.LogInformation("OrderTopicFunction::Run - Received message for OrderId {OrderId}", message.OrderId);

        var quote = await sender.Send(new BuildQuoteCommand(message, supplierOptions.Value.SupplierCode), cancellationToken);
        logger.LogInformation(
            "OrderTopicFunction::Run - Received Order Total Value {TotalValue} for OrderId {OrderId}",
            quote.TotalPrice,
            message.OrderId);

        await quotePublisher.PublishAsync(quote, cancellationToken);
        logger.LogInformation("OrderTopicFunction::Run - SUCCESSFULLY Processed Message");
    }
}
