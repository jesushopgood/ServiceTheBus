using common.Messaging;
using MediatR;
using Microsoft.Extensions.Options;
using SupplierENG.Application.Common.Interfaces;
using SupplierENG.Application.Features.Orders.BuildQuote;
using SupplierENG.Infrastructure.Messaging;

namespace SupplierENG.Api.Triggers;

public sealed class OrderTopicTriggerBackgroundService(
    IServiceScopeFactory scopeFactory,
    IQueueService queueService,
    IOptions<SupplierServiceBusOptions> options,
    ILogger<OrderTopicTriggerBackgroundService> logger) : BackgroundService
{
    private readonly ILogger<OrderTopicTriggerBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.SupplierCode))
        {
            _logger.LogInformation("Order topic trigger disabled: SupplierServiceBus:SupplierCode is missing.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await queueService.ProcessNextOrderAsync(
                    settings.SupplierCode,
                    async (message, cancellationToken) =>
                    {
                        _logger.LogInformation(
                            "OrderTopicTriggerBackgroundService::ExecuteAsync - Received message for OrderId {OrderId}",
                            message.OrderId);

                        using var scope = scopeFactory.CreateScope();
                        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                        var quotePublisher = scope.ServiceProvider.GetRequiredService<IQuotePublisher>();

                        var quote = await sender.Send(new BuildQuoteCommand(message, settings.SupplierCode), cancellationToken);

                        _logger.LogInformation(
                            "OrderTopicTriggerBackgroundService::ExecuteAsync - Received Order Total Value {TotalValue} for OrderId {OrderId}",
                            quote.TotalPrice,
                            message.OrderId);

                        await quotePublisher.PublishAsync(quote, cancellationToken);
                        _logger.LogInformation("OrderTopicTriggerBackgroundService::ExecuteAsync - SUCCESSFULLY Processed Message");
                    },
                    stoppingToken);

                if (!processed)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500), stoppingToken);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Supplier topic trigger processing error.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
