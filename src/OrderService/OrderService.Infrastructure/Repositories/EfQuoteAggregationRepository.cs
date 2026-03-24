using common.Messaging;
using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Entities;

namespace OrderService.Infrastructure.Repositories;

public sealed class EfQuoteAggregationRepository(OrderServiceDbContext dbContext) : IQuoteAggregationRepository
{
    public async Task<int> AddQuoteAsync(SupplierQuoteMessage quote, CancellationToken cancellationToken)
    {
        var existing = await dbContext.Quotes
            .FirstOrDefaultAsync(
                x => x.OrderId == quote.OrderId && x.ServiceName == quote.SupplierCode,
                cancellationToken);

        if (existing is null)
        {
            await dbContext.Quotes.AddAsync(new QuoteEntity
            {
                OrderId = quote.OrderId,
                ServiceName = quote.SupplierCode,
                TotalPrice = quote.TotalPrice,
                IsCheapest = false,
                ReceivedAtUtc = DateTime.UtcNow
            }, cancellationToken);
        }
        else
        {
            existing.TotalPrice = quote.TotalPrice;
            existing.ReceivedAtUtc = DateTime.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Quotes
            .CountAsync(x => x.OrderId == quote.OrderId, cancellationToken);
    }

    public async Task<IReadOnlyList<SupplierQuoteMessage>> GetQuotesAsync(Guid orderId, CancellationToken cancellationToken)
    {
        var quotes = await dbContext.Quotes
            .AsNoTracking()
            .Where(x => x.OrderId == orderId)
            .OrderBy(x => x.TotalPrice)
            .ToListAsync(cancellationToken);

        return quotes
            .Select(x => new SupplierQuoteMessage(
                x.OrderId,
                x.ServiceName,
                x.TotalPrice,
                Array.Empty<SupplierQuoteItemMessage>()))
            .ToList();
    }

    public async Task MarkCheapestAsync(Guid orderId, string supplierCode, CancellationToken cancellationToken)
    {
        var quotes = await dbContext.Quotes
            .Where(x => x.OrderId == orderId)
            .ToListAsync(cancellationToken);

        foreach (var quote in quotes)
        {
            quote.IsCheapest = quote.ServiceName.Equals(supplierCode, StringComparison.OrdinalIgnoreCase);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
