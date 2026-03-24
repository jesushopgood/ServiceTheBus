using common.Messaging;
using MediatR;
using SupplierENG.Application.Common.Interfaces;
using SupplierENG.Domain.Entities;

namespace SupplierENG.Application.Features.Orders.BuildQuote;

public sealed class BuildQuoteCommandHandler(IProductRepository productRepository)
    : IRequestHandler<BuildQuoteCommand, SupplierQuoteMessage>
{
    public async Task<SupplierQuoteMessage> Handle(BuildQuoteCommand request, CancellationToken cancellationToken)
    {
        var products = await productRepository.GetAllAsync(cancellationToken);
        var productBySku = products.ToDictionary(x => x.Sku, StringComparer.OrdinalIgnoreCase);

        var items = request.Order.Items
            .Where(item => productBySku.ContainsKey(item.Sku))
            .Select(item =>
            {
                var supplierProduct = productBySku[item.Sku];
                var quoteItem = new SupplierQuoteItem
                {
                    Sku = supplierProduct.Sku,
                    Description = supplierProduct.Description,
                    Quantity = item.Quantity,
                    UnitPrice = supplierProduct.BestPrice
                };

                return new SupplierQuoteItemMessage(
                    quoteItem.Sku,
                    quoteItem.Description,
                    quoteItem.Quantity,
                    quoteItem.UnitPrice,
                    quoteItem.LineTotal);
            })
            .ToList();

        var quoteEntity = new SupplierQuote
        {
            OrderId = request.Order.OrderId,
            SupplierCode = request.SupplierCode,
            Items = items.Select(x => new SupplierQuoteItem
            {
                Sku = x.Sku,
                Description = x.Description,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice
            }).ToList()
        };

        return new SupplierQuoteMessage(
            quoteEntity.OrderId,
            quoteEntity.SupplierCode,
            quoteEntity.TotalPrice,
            items);
    }
}
