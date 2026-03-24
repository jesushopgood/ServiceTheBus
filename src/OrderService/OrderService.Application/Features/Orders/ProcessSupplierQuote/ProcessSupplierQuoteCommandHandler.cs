using common.Messaging;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Application.Features.Orders.ProcessSupplierQuote;

public sealed class ProcessSupplierQuoteCommandHandler(
    IQuoteAggregationRepository quoteAggregationRepository,
    ILogger<ProcessSupplierQuoteCommandHandler> logger) : IRequestHandler<ProcessSupplierQuoteCommand>
{
    private const int ExpectedSuppliers = 3;

    public async Task Handle(ProcessSupplierQuoteCommand request, CancellationToken cancellationToken)
    {
        var receivedCount = await quoteAggregationRepository.AddQuoteAsync(request.Quote, cancellationToken);
        logger.LogInformation(
            "Received supplier quote for order {OrderId} at TotalPrice {TotalPrice} from {SupplierCode}. Total so far: {Count}",
            request.Quote.OrderId,
            request.Quote.TotalPrice,
            request.Quote.SupplierCode,
            receivedCount);

        if (receivedCount < ExpectedSuppliers)
        {
            return;
        }

        var quotes = await quoteAggregationRepository.GetQuotesAsync(request.Quote.OrderId, cancellationToken);
        var winner = quotes.OrderBy(q => q.TotalPrice).First();

        await quoteAggregationRepository.MarkCheapestAsync(request.Quote.OrderId, winner.SupplierCode, cancellationToken);

        logger.LogInformation(
            "Cheapest supplier for order {OrderId} is {SupplierCode} at {TotalPrice}",
            winner.OrderId,
            winner.SupplierCode,
            winner.TotalPrice);
    }
}
