using SupplierWAL.Domain.Entities;

namespace SupplierWAL.Application.Common.Interfaces;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken);
}
