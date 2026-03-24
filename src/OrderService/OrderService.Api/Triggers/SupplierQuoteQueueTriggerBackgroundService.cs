using common.Messaging;
using MediatR;
using OrderService.Application.Features.Orders.ProcessSupplierQuote;

namespace OrderService.Api.Triggers;

public sealed class SupplierQuoteQueueTriggerBackgroundService(
    ISender sender,
    IQueueService queueService,
    ILogger<SupplierQuoteQueueTriggerBackgroundService> logger) : BackgroundService
{
    private readonly ILogger<SupplierQuoteQueueTriggerBackgroundService> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await queueService.ProcessNextSupplierQuoteAsync(
                    async (quote, cancellationToken) =>
                    {
                        _logger.LogInformation(
                            "SupplierQuoteQueueTriggerBackgroundService::ExecuteAsync - Received Order Total Value {TotalValue} for OrderId {OrderId}",
                            quote.TotalPrice,
                            quote.OrderId);

                        await sender.Send(new ProcessSupplierQuoteCommand(quote), cancellationToken);

                        _logger.LogInformation("SupplierQuoteQueueTriggerBackgroundService::ExecuteAsync - SUCCESSFULLY Processed Message");
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
                _logger.LogError(ex, "Supplier quote trigger processing error.");
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
