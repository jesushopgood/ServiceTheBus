using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace common.Messaging;

public sealed class LiveQueueService(IConfiguration configuration, ILogger<LiveQueueService> logger) : IQueueService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task PublishOrderAsync(OrderPlacedMessage order, CancellationToken cancellationToken)
    {
        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        var entityName = ResolveOrderEntityName();

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(entityName))
        {
            throw new InvalidOperationException("AzureServiceBus order publishing settings are missing.");
        }

        await using var client = new ServiceBusClient(connectionString);
        await using ServiceBusSender sender = client.CreateSender(entityName);

        var payload = JsonSerializer.Serialize(order, SerializerOptions);
        var message = new ServiceBusMessage(payload)
        {
            MessageId = order.OrderId.ToString(),
            CorrelationId = order.OrderId.ToString(),
            Subject = "order.placed",
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken);
        logger.LogInformation("Published order {OrderId} to {EntityName}", order.OrderId, entityName);
    }

    public async Task PublishSupplierQuoteAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken)
    {
        var connectionString = configuration["SupplierServiceBus:ConnectionString"];
        var queueName = configuration["SupplierServiceBus:SupplierQuotesQueueName"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(queueName))
        {
            throw new InvalidOperationException("SupplierServiceBus queue publishing settings are missing.");
        }

        await using var client = new ServiceBusClient(connectionString);
        await using ServiceBusSender sender = client.CreateSender(queueName);

        var payload = JsonSerializer.Serialize(quote, SerializerOptions);
        var message = new ServiceBusMessage(payload)
        {
            MessageId = Guid.NewGuid().ToString(),
            CorrelationId = quote.OrderId.ToString(),
            Subject = "supplier.quote",
            ContentType = "application/json"
        };

        await sender.SendMessageAsync(message, cancellationToken);
        logger.LogInformation("Published quote for order {OrderId} from supplier {SupplierCode}", quote.OrderId, quote.SupplierCode);
    }

    public async Task<bool> ProcessNextOrderAsync(
        string supplierCode,
        Func<OrderPlacedMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        _ = supplierCode;

        var connectionString = configuration["SupplierServiceBus:ConnectionString"];
        var topicName = configuration["SupplierServiceBus:OrdersTopicName"];
        var subscriptionName = configuration["SupplierServiceBus:OrdersSubscriptionName"];

        if (string.IsNullOrWhiteSpace(connectionString) ||
            string.IsNullOrWhiteSpace(topicName) ||
            string.IsNullOrWhiteSpace(subscriptionName))
        {
            throw new InvalidOperationException("SupplierServiceBus topic subscription settings are missing.");
        }

        await using var client = new ServiceBusClient(connectionString);
        await using ServiceBusReceiver receiver = client.CreateReceiver(topicName, subscriptionName);

        ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1), cancellationToken);
        if (message is null)
        {
            return false;
        }

        var order = JsonSerializer.Deserialize<OrderPlacedMessage>(message.Body.ToString(), SerializerOptions);
        if (order is null)
        {
            await receiver.DeadLetterMessageAsync(message, cancellationToken: cancellationToken);
            return false;
        }

        try
        {
            await handler(order, cancellationToken);
            await receiver.CompleteMessageAsync(message, cancellationToken);
            return true;
        }
        catch
        {
            await receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
            throw;
        }
    }

    public async Task<bool> ProcessNextSupplierQuoteAsync(
        Func<SupplierQuoteMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        var connectionString = configuration["AzureServiceBus:ConnectionString"];
        var queueName = configuration["AzureServiceBus:SupplierQuotesQueueName"];

        if (string.IsNullOrWhiteSpace(connectionString) || string.IsNullOrWhiteSpace(queueName))
        {
            throw new InvalidOperationException("AzureServiceBus supplier quote queue settings are missing.");
        }

        await using var client = new ServiceBusClient(connectionString);
        await using ServiceBusReceiver receiver = client.CreateReceiver(queueName);

        ServiceBusReceivedMessage? message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(1), cancellationToken);
        if (message is null)
        {
            return false;
        }

        var quote = JsonSerializer.Deserialize<SupplierQuoteMessage>(message.Body.ToString(), SerializerOptions);
        if (quote is null)
        {
            await receiver.DeadLetterMessageAsync(message, cancellationToken: cancellationToken);
            return false;
        }

        try
        {
            await handler(quote, cancellationToken);
            await receiver.CompleteMessageAsync(message, cancellationToken);
            return true;
        }
        catch
        {
            await receiver.AbandonMessageAsync(message, cancellationToken: cancellationToken);
            throw;
        }
    }

    private string ResolveOrderEntityName()
    {
        var topicName = configuration["AzureServiceBus:TopicName"];
        if (!string.IsNullOrWhiteSpace(topicName))
        {
            return topicName;
        }

        return configuration["AzureServiceBus:QueueName"] ?? string.Empty;
    }
}
