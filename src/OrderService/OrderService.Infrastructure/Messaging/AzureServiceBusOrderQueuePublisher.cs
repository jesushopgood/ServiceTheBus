using System.Text.Json;
using Azure.Messaging.ServiceBus;
using common.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.Messaging;

public sealed class AzureServiceBusOrderQueuePublisher(
    IOptions<AzureServiceBusOptions> options,
    ILogger<AzureServiceBusOrderQueuePublisher> logger) : IOrderQueuePublisher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly AzureServiceBusOptions _options = options.Value;

    public async Task PublishAsync(OrderPlacedMessage order, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.ConnectionString))
        {
            throw new InvalidOperationException("AzureServiceBus:ConnectionString is not configured.");
        }

        var entityName = ResolveEntityName();
        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new InvalidOperationException("AzureServiceBus:TopicName is not configured.");
        }

        var clientOptions = BuildClientOptions();
        await using var client = new ServiceBusClient(_options.ConnectionString, clientOptions);
        ServiceBusSender sender = client.CreateSender(entityName);

        var payload = JsonSerializer.Serialize(order, SerializerOptions);
        var message = new ServiceBusMessage(payload)
        {
            MessageId = order.OrderId.ToString(),
            CorrelationId = order.OrderId.ToString(),
            ContentType = "application/json",
            Subject = "order.placed"
        };

        await sender.SendMessageAsync(message, cancellationToken);
        logger.LogInformation("Published order {OrderId} with {TotalItems} items to {EntityName}", order.OrderId, order.Items.Count, entityName);
    }

    private string ResolveEntityName()
    {
        if (!string.IsNullOrWhiteSpace(_options.TopicName))
        {
            return _options.TopicName;
        }

        return _options.QueueName;
    }

    private ServiceBusClientOptions BuildClientOptions()
    {
        var clientOptions = new ServiceBusClientOptions
        {
            TransportType = ParseTransportType(_options.TransportType)
        };

        if (_options.Retry.MaxRetries is > 0)
        {
            clientOptions.RetryOptions.MaxRetries = _options.Retry.MaxRetries.Value;
        }

        if (_options.Retry.DelayMs is > 0)
        {
            clientOptions.RetryOptions.Delay = TimeSpan.FromMilliseconds(_options.Retry.DelayMs.Value);
        }

        if (_options.Retry.TryTimeoutSeconds is > 0)
        {
            clientOptions.RetryOptions.TryTimeout = TimeSpan.FromSeconds(_options.Retry.TryTimeoutSeconds.Value);
        }

        return clientOptions;
    }

    private static ServiceBusTransportType ParseTransportType(string? configuredTransportType)
    {
        return configuredTransportType?.Trim().ToLowerInvariant() switch
        {
            "amqpwebsockets" => ServiceBusTransportType.AmqpWebSockets,
            _ => ServiceBusTransportType.AmqpTcp
        };
    }
}
