namespace OrderService.Infrastructure.Messaging;

public sealed class AzureServiceBusOptions
{
    public const string SectionName = "AzureServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string TopicName { get; init; } = string.Empty;

    public string SupplierQuotesQueueName { get; init; } = string.Empty;

    // Backward compatibility while transitioning from queue to topic.
    public string QueueName { get; init; } = string.Empty;

    public AzureServiceBusRetryOptions Retry { get; init; } = new();

    public string TransportType { get; init; } = "AmqpTcp";
}

public sealed class AzureServiceBusRetryOptions
{
    public int? MaxRetries { get; init; }

    public int? DelayMs { get; init; }

    public int? TryTimeoutSeconds { get; init; }
}
