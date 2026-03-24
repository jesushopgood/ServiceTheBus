using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Api.Specs.Support;

public sealed class InMemoryProductRepository : IProductRepository
{
    private static readonly IReadOnlyList<Product> Products =
    [
        new Product { Id = 1, Sku = "SK1", Description = "Dinner Plate", BasePrice = 12.50m },
        new Product { Id = 2, Sku = "SK2", Description = "Side Plate", BasePrice = 8.25m }
    ];

    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Products);
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        var product = Products.FirstOrDefault(p => p.Id == id);
        return Task.FromResult(product);
    }
}
