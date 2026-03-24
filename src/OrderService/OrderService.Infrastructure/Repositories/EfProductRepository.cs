using Microsoft.EntityFrameworkCore;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.Repositories;

public sealed class EfProductRepository(OrderServiceDbContext dbContext) : IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        var products = await dbContext.Products
            .AsNoTracking()
            .OrderBy(x => x.Id)
            .Select(x => new Product
            {
                Id = x.Id,
                Sku = x.Sku,
                Description = x.Description,
                BasePrice = x.BasePrice
            })
            .ToListAsync(cancellationToken);

        return products;
    }

    public async Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return await dbContext.Products
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new Product
            {
                Id = x.Id,
                Sku = x.Sku,
                Description = x.Description,
                BasePrice = x.BasePrice
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}
