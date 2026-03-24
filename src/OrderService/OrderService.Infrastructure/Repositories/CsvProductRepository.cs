using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Repositories;

[Obsolete("CsvProductRepository has been replaced by EF Core persistence. Use EfProductRepository.")]
public sealed class CsvProductRepository(EfProductRepository innerRepository) : IProductRepository
{
    public Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken)
    {
        return innerRepository.GetAllAsync(cancellationToken);
    }

    public Task<Product?> GetByIdAsync(int id, CancellationToken cancellationToken)
    {
        return innerRepository.GetByIdAsync(id, cancellationToken);
    }
}
