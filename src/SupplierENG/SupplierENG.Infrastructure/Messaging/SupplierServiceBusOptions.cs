namespace SupplierENG.Infrastructure.Messaging;

public sealed class SupplierServiceBusOptions
{
    public const string SectionName = "SupplierServiceBus";

    public string ConnectionString { get; init; } = string.Empty;

    public string OrdersTopicName { get; init; } = string.Empty;

    public string OrdersSubscriptionName { get; init; } = string.Empty;

    public string SupplierQuotesQueueName { get; init; } = string.Empty;

    public string SupplierCode { get; init; } = string.Empty;

    public string ProductsCsvPath { get; init; } = "../products.csv";
}
